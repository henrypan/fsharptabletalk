module SodaServiceDefinition

open System
open FSharp.Data.JsonExtensions
open FSharp.Data

type SodaCommand = 
  | Request of id: string * uri : string 
  | Synchronize of AsyncReplyChannel<bool>

type SodaEvent =
  | Response of id: string * contents: JsonValue

type SodaState =
  | Nothing

type BuildingPermitRaw =
  { action_type : string option
    latitude: float
    longitude: float
    permit_and_complaint_status_url: string
    permit_type: string
    work_type: string }

let event =
  new Event<SodaEvent>()

let publishedEvent = event.Publish

let tryString (x: JsonValue) (field: string): string option =
  x.TryGetProperty field
  |> Option.map (fun y -> y.AsString())

let convertToBuildingPermitRaw (x: JsonValue) =
  { BuildingPermitRaw.action_type = tryString x "action_type"
    latitude = x?latitude.AsFloat()
    longitude = x?longitude.AsFloat()
    permit_and_complaint_status_url = x?permit_and_complaint_status_url.AsString()
    permit_type = x?permit_type.AsString()
    work_type= x?work_type.AsString() }

let rec serve state (mbox : MailboxProcessor<SodaCommand>): Async<Unit>  = 
  async {  
    let continuation events = 
      events
      |> Seq.iter event.Trigger
      serve state mbox
    let continuationWithNoEvents =
      continuation []
    let! msg = mbox.Receive()
    match msg with
    | SodaCommand.Request(id, uri) ->
      let! result = JsonValue.AsyncLoad("https://data.seattle.gov/resource/i5jq-ms7b.json")
      let longitudes =
        result.AsArray()
        |> Seq.map convertToBuildingPermitRaw
      printfn "outfs %A" longitudes
      return! continuation [SodaEvent.Response(id, result)]
    | SodaCommand.Synchronize replyChannel ->
      replyChannel.Reply(true)
      return! continuationWithNoEvents
  }

let actor: MailboxProcessor<SodaCommand> =
  let state = 1
  MailboxProcessor.Start(serve SodaState.Nothing)