module AgnetThor

open System
open System.Collections.Generic
open System.Reactive
open System.Reactive.Linq
open System.Net
open System.Text
open System.Text.RegularExpressions
// mplements a simple agent that lets you throttle the degree of parallelism by limiting the
// number of work items that are processed in parallel.

//Agent that can be used for controlling the number of concurrently executing asynchronous workflows.
// The agent runs a specified number of operations concurrently and queues remaining pending requests.
// The queued work items are started as soon as one of the previous items completes.

type Agent<'a> = MailboxProcessor<'a>

/// Message type used by the agent - contains queueing
/// of work items and notification of completion
type JobRequest<'T, 'R> =
    | Ask of 'T * AsyncReplyChannel<'R>
    | Completed
    | Quit

/// Represents an agent that runs operations in concurrently. When the number
/// of concurrent operations exceeds 'limit', they are queued and processed later
type ThrottlingAgent<'T, 'R>(limit, operation:'T -> Async<'R>) =
    let jobCompleted = new Event<'R>()

    let agent = Agent<JobRequest<'T, 'R>>.Start(fun agent ->
        let dispose() = (agent :> IDisposable).Dispose()
    /// Represents a state when the agent is working
        let rec running jobCount = async {
      // Receive any message
          let! msg = agent.Receive()
          match msg with
          | Quit -> dispose()
          | Completed ->
          // Decrement the counter of work items
              return! running (jobCount - 1)
          // Start the work item & continue in blocked/working state
          | Ask(job, reply) ->
               do!
                 async { try
                             let! result = operation job
                             jobCompleted.Trigger result
                             reply.Reply(result)
                         finally agent.Post(Completed) }
               |> Async.StartChild |> Async.Ignore
               if jobCount < limit - 1 then return! running (jobCount + 1)
               else return! idle ()
    /// Represents a state when the agent is blocked
            }
        and idle () =
      // Use 'Scan' to wait for completion of some work
              agent.Scan(function
              | Completed -> Some(running (limit - 1))
              | _ -> None)
    // Start in working state with zero running work items
        running 0)

  /// Queue the specified asynchronous workflow for processing
    member x.Ask(job) = agent.PostAndAsyncReply(fun ch -> Ask(job, ch))

    member x.Subsrcibe(action) = jobCompleted.Publish |> Observable.subscribe(action)

let pipelineAgent l f job : Async<_> =
    let a = ThrottlingAgent(l, f)
    a.Ask(job)

module Async =

    //let bind f xAsync = async {
    //    let! x = xAsync
    //    return f x }

    let retn x = async { return x }

let bind f xAsync = async {
    let! x = xAsync
    return! f x }

let agent1 = pipelineAgent 2 (fun (x:string) -> async { return System.Int32.Parse x })
let agent2 = pipelineAgent 2 (fun (x:int) -> async.Return x)

let (>>=) x f = bind f x // async.Bind(x, f)
let pipeline x = Async.retn x >>= agent1 >>= agent2

let (>=>) f g x = (f x) >>= g

//let (>=>) f1 f2 x = f1 x >>= f2

let pipeline2 = agent1 >=> agent2


//let throttle= ThrottlingAgent(2, id)

type Message<'a, 'b> = 'a * AsyncReplyChannel<'b>
let agent f = Agent<Message<'a,'b>>.Start(fun inbox ->
    let rec loop () = async {
        let! msg, replyChannel = inbox.Receive()

        replyChannel.Reply (f msg)

        return! loop() }
    loop() )

let pipelineAgent2 f m =
    let a = agent f
    a.PostAndAsyncReply(fun replyChannel -> m, replyChannel)

let agent1' = pipelineAgent2 (fun (x:int) -> printfn "Pipeline processing 1: %d" x; "")
let agent2' = pipelineAgent2 (fun x -> printfn "Pipeline processing 2: %s" x; x)

let message = "Message in the pipeline"

