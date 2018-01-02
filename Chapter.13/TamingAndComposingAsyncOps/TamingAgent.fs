module TamingAgentModule

open System
open System.Collections.Generic
open System.Net
open System.Text
open System.Text.RegularExpressions

type Agent<'a> = MailboxProcessor<'a>

// Listing 13.17 Implementation of the TamingAgent
// Message type used by the agent - contains queuing
// of work items and notification of completion
type JobRequest<'T, 'R> = // #A
    | Ask of 'T * AsyncReplyChannel<'R>
    | Completed
    | Quit

// Implements an agent that lets you throttle the degree of parallelism by limiting the
// number of work items that are processed in parallel.
type TamingAgent<'T, 'R>(limit, operation:'T -> Async<'R>) =
    let jobCompleted = new Event<'R>() // #B

    let tamingAgent = Agent<JobRequest<'T, 'R>>.Start(fun agent ->
        let dispose() = (agent :> IDisposable).Dispose() //#C
        /// Represents a state when the agent is working
        let rec running jobCount = async { //#D
          // Receive any message
          let! msg = agent.Receive()
          match msg with
          | Quit -> dispose()
          | Completed ->
              // Decrement the counter of work items
              return! running (jobCount - 1) // #E
          // Start the work item & continue in blocked/working state
          | Ask(job, reply) -> // #F
               do!
                 async { try
                             let! result = operation job // #G
                             jobCompleted.Trigger result // #H
                             reply.Reply(result) // #I
                         finally agent.Post(Completed) } // #L
               |> Async.StartChild |> Async.Ignore // #M
               if jobCount <= limit - 1 then return! running (jobCount + 1)
               else return! idle () // #N
            /// Represents a state when the agent is blocked
            }
        and idle () = // #O
              // Use 'Scan' to wait for completion of some work
              agent.Scan(function //#N
              | Completed -> Some(running (limit - 1))
              | _ -> None)
        // Start in working state with zero running work items
        running 0) // #P

    /// Queue the specified asynchronous workflow for processing
    member this.Ask(value) = tamingAgent.PostAndAsyncReply(fun ch -> Ask(value, ch)) // #Q
    member this.Stop() = tamingAgent.Post(Quit)
    member x.Subscribe(action) = jobCompleted.Publish |> Observable.subscribe(action) // #R


