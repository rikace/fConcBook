module Agents4DB

open System.Collections.Generic
open System.Threading
open System

type Agent<'T> = MailboxProcessor<'T>

type AgentDisposable<'T> (behavior:MailboxProcessor<'T> -> Async<unit>,
                          ?cancelToken:CancellationTokenSource) =
    let cancelToken = defaultArg cancelToken (new CancellationTokenSource())
    let agent = MailboxProcessor.Start(behavior, cancelToken.Token)

    member __.Agent = agent
    member this.reportErrorsTo (supervisor: Agent<exn>) =
        this.Agent.Error.Add(supervisor.Post); this

    interface IDisposable with
        member __.Dispose() =
            (agent :> IDisposable).Dispose()
            cancelToken.Cancel()


type AsyncReplyChannelWithAck<'a>(ch:AsyncReplyChannel<'a>, ack) =
    member this.Reply msg =
        ch.Reply(msg)
        ack()

// ---------------

type Person  =
    { id:int; firstName:string; lastName:string; age:int }

type SqlReadMessages =
    | Get of id:int * AsyncReplyChannelWithAck<Person option>
type SqlWriteMessages =
    | Add of person:Person * AsyncReplyChannelWithAck<int option>

type ReadWriteMessages<'r,'w> =
    | Read of r:'r
    | Write of w:'w

type ReaderWriterMag<'r,'w> =
    | Command of r:ReadWriteMessages<'r,'w>
    | CommandCompleted
and ReaderWriterGateState =
    | SendWrite
    | SendRead of cnt:int
    | Idle

type ReaderWriterAgent<'r,'w>
            (workers:int, behavior: MailboxProcessor<ReadWriteMessages<'r,'w>> -> Async<unit>,
             ?errorHandler, ?cts:CancellationTokenSource) =

    let cts = defaultArg cts (new System.Threading.CancellationTokenSource())
    let errorHandler = defaultArg errorHandler ignore
    let supervisor =
        Agent<Exception>.Start(fun inbox -> async {
            while true do
                let! error = inbox.Receive()
                errorHandler error })

    let agent = MailboxProcessor<ReaderWriterMag<'r,'w>>.Start((fun inbox ->
        let agents = Array.init workers (fun _ ->
            (new AgentDisposable<ReadWriteMessages<'r,'w>>(behavior, cts))
                .reportErrorsTo supervisor)

        cts.Token.Register(fun () ->
            agents |> Array.iter(fun agent -> (agent :> IDisposable).Dispose()))
        |> ignore

        let writeQueue = Queue<_>()
        let readQueue = Queue<_>()

        let rec loop i state = async {
            let! msg = inbox.Receive()
            let next = (i+1) % workers
            match msg with
            | Command(Read(req)) ->
                match state with
                | Idle ->
                    agents.[i].Agent.Post(Read(req))
                    return! loop next (SendRead 1)
                | SendRead(n) when writeQueue.Count = 0 ->
                    agents.[i].Agent.Post(Read(req))
                    return! loop next (SendRead(n+1))
                | _ ->
                    readQueue.Enqueue(req)
                    return! loop i state
            | Command(Write(req)) ->
                match state with
                | Idle ->
                    agents.[i].Agent.Post(Write(req))
                    return! loop next SendWrite
                | SendRead(_) | SendWrite ->
                    writeQueue.Enqueue(req)
                    return! loop i state
            | CommandCompleted ->
                match state with
                | Idle -> failwith "Operation no possible"
                | SendRead(n) when n > 1 ->
                    return! loop i (SendRead(n-1))
                | SendWrite | SendRead(_) ->
                    if writeQueue.Count > 0 then
                        let req = writeQueue.Dequeue()
                        agents.[i].Agent.Post(Write(req))
                        return! loop next SendWrite
                    elif readQueue.Count > 0 then
                        readQueue |> Seq.iteri (fun j req ->
                            agents.[(i+j)%workers].Agent.Post(Read(req)))
                        let cnt = readQueue.Count
                        readQueue.Clear()
                        return! loop ((i+cnt)%workers) (SendRead cnt)
                    else
                        return! loop i Idle }
        loop 0 Idle), cts.Token)

    let postAndAsyncReply cmd createRequest =
        agent.PostAndAsyncReply(fun ch ->
            createRequest(AsyncReplyChannelWithAck(ch, fun () -> agent.Post(CommandCompleted)))
            |> cmd |> ReaderWriterMag.Command)

    member this.Read(createReadRequest)   = postAndAsyncReply Read  createReadRequest
    member this.Write(createWriteRequest) = postAndAsyncReply Write createWriteRequest



