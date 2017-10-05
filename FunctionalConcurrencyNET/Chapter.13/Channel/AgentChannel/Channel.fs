module Channel

open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading.Tasks
open System.Linq
open System.Threading

type private Context = {cont:unit -> unit; context:ExecutionContext}

type TaskPool private () =
    let cts = new CancellationTokenSource()
    let queue = Array.init 2 (fun _ -> new BlockingCollection<Context>(ConcurrentQueue()))
    let work() =
        while queue.All(fun bc -> bc.IsCompleted) |> not && cts.IsCancellationRequested |> not do
        let ctx = ref Unchecked.defaultof<Context>
        if BlockingCollection<_>.TryTakeFromAny(queue, ctx) >= 0 then
            let ctx = ctx.Value
            let ec = ctx.context.CreateCopy()
            ExecutionContext.Run(ec, (fun _ -> ctx.cont()), null)
    let long = TaskCreationOptions.LongRunning
    let tasks = Array.init 1 (fun _ -> new Task(work, cts.Token, long))
    do tasks |> Array.iter(fun task -> task.Start())

    static let self = TaskPool()
    member private this.Stop() =
        for bc in queue do bc.CompleteAdding()
        cts.Cancel()
    member private this.Add continutaion =
        let ctx = {cont = continutaion; context = ExecutionContext.Capture() }
        BlockingCollection<_>.TryAddToAny(queue, ctx) |> ignore
    static member Add(continuation:unit -> unit) = self.Add continuation
    static member Stop() = self.Stop()

type internal ChannleMsg<'a> =
    | Recv of ('a -> unit) * AsyncReplyChannel<unit>
    | Send of 'a * (unit -> unit) * AsyncReplyChannel<unit>

[<Sealed>]
type ChannelAgent<'a>() =
    let agent = MailboxProcessor<ChannleMsg<'a>>.Start(fun inbox ->
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
                    let (value, cont) =writers.Dequeue()
                    TaskPool.Add cont
                    reply.Reply( (ok value) )
                return! loop()
            | Send(x, ok, reply) ->
                if readers.Count = 0 then
                    writers.Enqueue(x, ok)
                    reply.Reply( () )
                else
                    let cont = readers.Dequeue()
                    TaskPool.Add ok
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