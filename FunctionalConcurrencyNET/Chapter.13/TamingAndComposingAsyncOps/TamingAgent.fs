module TamingAgentModule

open System
open System.Collections.Generic
open System.Net
open System.Text
open System.Text.RegularExpressions
type Agent<'a> = MailboxProcessor<'a>

type JobRequest<'T, 'R> =
    | Ask of 'T * AsyncReplyChannel<'R>
    | Completed
    | Quit

type TamingAgent<'T, 'R>(limit, operation:'T -> Async<'R>) =
    let jobCompleted = new Event<'R>()

    let tamingAgent = Agent<JobRequest<'T, 'R>>.Start(fun agent ->
        let dispose() = (agent :> IDisposable).Dispose()
        let rec running jobCount = async {
          let! msg = agent.Receive()
          match msg with
          | Quit -> dispose()
          | Completed ->
              return! running (jobCount - 1)
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
            }
        and idle () =
              agent.Scan(function
              | Completed -> Some(running (limit - 1))
              | _ -> None)
        running 0)

    member this.Ask(value) = tamingAgent.PostAndAsyncReply(fun ch -> Ask(value, ch))
    member this.Stop() = tamingAgent.Post(Quit)
    member x.Subsrcibe(action) = jobCompleted.Publish |> Observable.subscribe(action)
