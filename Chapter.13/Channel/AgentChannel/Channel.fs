module Channel

open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading.Tasks
open System.Linq
open System.Threading
open ParallelWorkersAgent

//Listing 13.16 ChannelAgent for CSP implementation using MailboxProcessor
type private Context = {cont:unit -> unit; context:ExecutionContext}

type TaskPool private (numWorkers) =
    let worker (inbox: MailboxProcessor<Context>) =
        let rec loop() = async {
            let! ctx = inbox.Receive()
            let ec = ctx.context.CreateCopy()
            ExecutionContext.Run(ec, (fun _ -> ctx.cont()), null)
            return! loop() }
        loop()
    let agent = MailboxProcessor<Context>.parallelWorker(numWorkers, worker)

    static let self = TaskPool(2)
    member private this.Add continutaion =
        let ctx = {cont = continutaion; context = ExecutionContext.Capture() }
        agent.Post(ctx)
    static member Spawn (continuation:unit -> unit) = self.Add continuation

//Listing 13.15 ChannelAgent for CSP implementation using MailboxProcessor
type internal ChannelMsg<'a> =
    | Recv of ('a -> unit) * AsyncReplyChannel<unit>
    | Send of 'a * (unit -> unit) * AsyncReplyChannel<unit>

type [<Sealed>] ChannelAgent<'a>() =
    let agent = MailboxProcessor<ChannelMsg<'a>>.Start(fun inbox ->
        let readers = Queue<'a -> unit>()
        let writers = Queue<'a * (unit -> unit)>()

        let rec loop() = async {
            let! msg = inbox.Receive()
            match msg with
            | Recv(ok , reply) ->
                if writers.Count = 0 then
                    readers.Enqueue ok
                    reply.Reply( () )
                else
                    let (value, cont) = writers.Dequeue()
                    TaskPool.Spawn  cont
                    reply.Reply( (ok value) )
                return! loop()
            | Send(x, ok, reply) ->
                if readers.Count = 0 then
                    writers.Enqueue(x, ok)
                    reply.Reply( () )
                else
                    let cont = readers.Dequeue()
                    TaskPool.Spawn ok
                    reply.Reply( (cont x) )
                return! loop() }
        loop())

    member this.Recv(ok: 'a -> unit)  =
        agent.PostAndAsyncReply(fun ch -> Recv(ok, ch)) |> Async.Ignore

    member this.Send(value: 'a, ok:unit -> unit)  =
        agent.PostAndAsyncReply(fun ch -> Send(value, ok, ch)) |> Async.Ignore

    member this.Recv() =
        Async.FromContinuations(fun (ok, _,_) ->
            agent.PostAndAsyncReply(fun ch -> Recv(ok, ch)) |> Async.RunSynchronously)

    member this.Send (value:'a) =
        Async.FromContinuations(fun (ok, _,_) ->
            agent.PostAndAsyncReply(fun ch -> Send(value, ok, ch)) |> Async.RunSynchronously )

let run (action:Async<_>) = action |> Async.Ignore |> Async.Start

let rec subscribe (chan:ChannelAgent<_>) (handler:'a -> unit) =
    chan.Recv(fun value -> handler value
                           subscribe chan handler) |> run