module HostTest

open System
open NUnit.Framework
open FsUnit
open SodaServiceDefinition

[<Test>]
let ``some test``() =
    10.1 |> should (equalWithin 0.1) 10.11
    Assert.Fail("diediedie")

[<Test>]
let ``request url``() =
    let actor = SodaServiceDefinition.actor
    let items = SodaServiceDefinition.publishedEvent.Subscribe (fun xx -> System.Console.WriteLine ("gotx: " + xx.ToString()));
    let parameters =     
      { SodaRequestParameters.baseUrl = "https://data.seattle.gov/resource/i5jq-ms7b.json"
        offset = 0<offset>
        limit = 100<limit>
        where = "latitude > 0 and application_date is not null"
        order = "application_date desc" }
    SodaCommand.Request (parameters)
    |> actor.Post
    Threading.Thread.Sleep(1000)
    let task =
      async {
        let! sync = actor.PostAndAsyncReply SodaCommand.Synchronize
        return ()
      }
    Async.RunSynchronously task
    printfn "fnx all done"
    ()
