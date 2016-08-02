module SodaDataProviderModule

open SodaServiceDefinition
open System
open System.Reactive.Linq

type SodaDataProvider() = 
  member x.buildingPermitsReplayx : IObservable<BuildingPermit []> = 
    SodaServiceDefinition.replayEvent |> Observable.choose (function 
                                           | SodaEvent.OrderedItems(fromLat, 
                                                                    fromLon, 
                                                                    outputItems) -> 
                                             //printfn "fnx received n ordered items %d" (outputItems.Length)
                                             outputItems
                                             |> Array.take 10
                                             |> Some
                                           | _ -> None)
  
  //member x.gox() = 
  //  { SodaRequestParameters.baseUrl = 
  //      "https://data.seattle.gov/resource/i5jq-ms7b.json"
  //    offset = 0<offset>
  //    limit = 100<limit>
  //    where = "latitude is not null and application_date is not null"
  //    order = "application_date desc" }
  //  |> SodaCommand.Request
  //  |> x.permitActor.Post