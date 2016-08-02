module SodaServiceDefinition

open FSharp.Data
open FSharp.Data.JsonExtensions
open Flurl
open System
open System.Reactive.Linq

// We're using F# units of measure as another layer of types, not
// as actual units of measure
[<Measure>]
type offset

[<Measure>]
type limit

let addLimitToOffset (offset : int<offset>) (limit : int<limit>) : int<offset> = 
  offset + int limit * 1<offset>

type BuildingPermit = 
  { ActionType : string option
    StreetAddress : string option
    Latitude : decimal
    Longitude : decimal
    PermitType : string
    PermitUrl : string }

type SodaRequestParameters = 
  { baseUrl : string
    offset : int<offset>
    limit : int<limit>
    where : string
    order : string }

type SodaCommand = 
  | Request of parameters : SodaRequestParameters
  | ReceivedItems of items : BuildingPermit [] * parameters : SodaRequestParameters
  | Synchronize of AsyncReplyChannel<bool>
  | GetOrderedItems of fromLat: decimal * fromLon: decimal

type SodaEvent = 
  | ProcessedPartialResponse of contents : BuildingPermit [] * parameters : SodaRequestParameters
  | CompletedResponse of parameters : SodaRequestParameters
  | StartedRequestCycle of parameters : SodaRequestParameters
  | OrderedItems of fromLat: decimal * fromLon: decimal * items: BuildingPermit[]

type SodaState = 
  | Initial
  | Fetching of task : Threading.Tasks.Task<unit> * items: BuildingPermit[]

// Utilities ---------------------------------------------

// We're mixing lat and lon; for this sample, it's not important
let inline manhattanDistance aLat aLon bLat bLon =
  abs (bLat - aLat) + abs (bLon - aLon)

let manhattanDistanceForBuildingPermit fromLat fromLon (a : BuildingPermit): decimal = 
  manhattanDistance fromLat fromLon a.Latitude a.Longitude

let getOrElse defaultValue (x : 't option): 't = 
  match x with
  | Some result -> result
  | None -> defaultValue

let getOrElse0 = getOrElse 0m

// Used for debug printfs
let stateItemCount: SodaState -> int =
  function
  | SodaState.Initial -> -1
  | SodaState.Fetching(_, items) ->
    items.Length

// Our URL endpoint at Socrata 
let baseUrl = "https://data.seattle.gov/resource/i5jq-ms7b.json"

let buildUrl (requestParameters : SodaRequestParameters) : Url = 
  requestParameters.baseUrl
    .SetQueryParam("$limit", requestParameters.limit)
    .SetQueryParam("$offset", requestParameters.offset)
    .SetQueryParam("$where", requestParameters.where)
    .SetQueryParam("$order", requestParameters.order)

// Using F# Data Providers makes for faster development
// http://fsharp.github.io/FSharp.Data/library/JsonProvider.html
type BuildingPermitJsonProvider = JsonProvider< """SampleData.json""" >

let fetcher (parameters : SodaRequestParameters) = 
  async { 
    let url = buildUrl parameters
    //printfn "fnx about to fetch from server: %A / %A" parameters url
    let! fetchResult = BuildingPermitJsonProvider.AsyncLoad(string url)
    //printfn "fnx got fetch result %A %s" fetchResult 
      //(fetchResult.GetType().ToString())
    let processedResult = 
      fetchResult
      |> Seq.filter (fun x -> x.Latitude.IsSome && x.Longitude.IsSome)
      |> Seq.map (fun x -> 
           { BuildingPermit.ActionType = x.ActionType
             StreetAddress = x.Address
             Latitude = x.Latitude |> getOrElse0
             Longitude = x.Longitude |> getOrElse0
             PermitType = x.PermitType
             PermitUrl = x.PermitAndComplaintStatusUrl })
      |> Array.ofSeq
    //printfn "fnx result from query %A is %A" parameters processedResult
    return processedResult
  }

let rec continuousRequests (actor : MailboxProcessor<SodaCommand>) 
        (parameters : SodaRequestParameters) : Async<unit> = 
  async { 
    let! (items : BuildingPermit []) = fetcher parameters
    SodaCommand.ReceivedItems(items, parameters) |> actor.Post
    if (items
        |> Array.length > 0) then 
      let newParameters = 
        { parameters with offset = 
                            addLimitToOffset parameters.offset parameters.limit }
      //do! Async.Sleep(1000)
      return! continuousRequests actor newParameters
    else ()
  }

// Events and observables ---------------------------------------------

let private event = new Event<SodaEvent>()
let publishedEvent = event.Publish

let (replayEvent : IObservable<SodaEvent>, private replayEventSubscription) = 
  let a = Observable.Replay(publishedEvent, 1)
  (a :> IObservable<SodaEvent>, a.Connect())

let buildingPermitsReplay : IObservable<BuildingPermit []> = 
    replayEvent |> Observable.choose (function 
                                           | SodaEvent.OrderedItems(fromLat, 
                                                                    fromLon, 
                                                                    outputItems) -> 
                                             //printfn "fnx received n ordered items %d" (outputItems.Length)
                                             outputItems
                                             |> Array.take 10
                                             |> Some
                                           | _ -> None)
  

// Service ---------------------------------------------

let rec serve (state : SodaState) (mbox : MailboxProcessor<SodaCommand>) : Async<Unit> = 
  async { 
    let continuation newState events : Async<unit> = 
      events |> Seq.iter event.Trigger
      serve newState mbox
    
    let continuationWithNoEvents newState = continuation newState []
    let continueUnchanged = continuationWithNoEvents state
    let! msg = mbox.Receive()
    //printfn "fnx SodaService: got msg %A for item count %d state %A" msg (stateItemCount state) state
    match msg with
    | SodaCommand.Request(requestParameters) -> 
      match state with
      | SodaState.Initial -> 
        let remoteRequests = continuousRequests actor requestParameters
        let requestTask = Async.StartAsTask remoteRequests
        return! continuation (SodaState.Fetching(requestTask, Array.empty))
                  [ SodaEvent.StartedRequestCycle(requestParameters) ]
      | SodaState.Fetching _ -> return! continueUnchanged
    | SodaCommand.ReceivedItems(items, parameters) -> 
      let newState =
        match state with
        | SodaState.Fetching(currentTask, existingItems) ->
          SodaState.Fetching(currentTask, Array.append existingItems items)
        | Initial -> Initial
      return! continuation newState 
                [ SodaEvent.ProcessedPartialResponse(items, parameters) ]
    | GetOrderedItems(fromLat, fromLon) ->
      let distanceFrom = manhattanDistanceForBuildingPermit fromLat fromLon
      let sendItems unorderedItems: BuildingPermit[] =
        unorderedItems
        |> Array.sortBy distanceFrom
        |> Array.take 50
      let outputItems =
        match state with
        | SodaState.Fetching(_, items) ->
          sendItems items
        | SodaState.Initial ->
          Array.empty
      //printfn "fnx send items %A" outputItems
      return! continuation state 
                [ SodaEvent.OrderedItems(fromLat, fromLon, outputItems) ]
    | SodaCommand.Synchronize replyChannel -> 
      replyChannel.Reply(true)
      return! continueUnchanged
  }

and actor : MailboxProcessor<SodaCommand> = 
  MailboxProcessor.Start(serve SodaState.Initial)
