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
    actor.Post (SodaCommand.Request ("1", "adsf"));
    let task =
      async {
        let! sync = actor.PostAndAsyncReply SodaCommand.Synchronize
        return ()
      }
    let result = Async.RunSynchronously task
    printfn "got full result: %A" result

