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
    member this.withSupervisor (supervisor: Agent<exn>) =
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


// Listing 13.4  ReaderWriterAgent coordinator of asynchronous read and write operations
type ReaderWriterMsg<'r,'w> = //#A
    | Command of r:ReadWriteMsg<'r,'w>
    | CommandCompleted
and ReaderWriterGateState = // #B
    | SendWrite
    | SendRead of cnt:int
    | Idle
and ReadWriteMsg<'r,'w> = // #B
    | Read of r:'r
    | Write of w:'w

type ReaderWriterAgent<'r,'w>
            (workers:int, behavior: MailboxProcessor<ReadWriteMsg<'r,'w>> -> Async<unit>,
             ?errorHandler, ?cts:CancellationTokenSource) = // #C

    let cts = defaultArg cts (new System.Threading.CancellationTokenSource())
    let errorHandler = defaultArg errorHandler ignore
    let supervisor =
        Agent<Exception>.Start(fun inbox -> async {
            while true do // #D
                let! error = inbox.Receive()
                errorHandler error })

    let agent = MailboxProcessor<ReaderWriterMsg<'r,'w>>.Start((fun inbox ->
        let agents = Array.init workers (fun _ -> // #E
            (new AgentDisposable<ReadWriteMsg<'r,'w>>(behavior, cts))
                .withSupervisor supervisor)

        cts.Token.Register(fun () ->
            agents |> Array.iter(fun agent -> (agent :> IDisposable).Dispose()))
        |> ignore

        let writeQueue = Queue<_>() // #F
        let readQueue = Queue<_>()  // #F

        let rec loop i state = async {
            let! msg = inbox.Receive()
            let next = (i+1) % workers // #G
            match msg with
            | Command(Read(req)) ->
                match state with // #H
                | Idle ->
                    agents.[i].Agent.Post(Read(req))
                    return! loop next (SendRead 1)
                | SendRead(n) when writeQueue.Count = 0 ->
                    agents.[i].Agent.Post(Read(req))
                    return! loop next (SendRead(n+1))
                | _ ->
                    readQueue.Enqueue(req)
                    return! loop i state
            | Command(Write(req)) -> // #H
                match state with
                | Idle ->
                    agents.[i].Agent.Post(Write(req))
                    return! loop next SendWrite
                | SendRead(_) | SendWrite ->
                    writeQueue.Enqueue(req)
                    return! loop i state
            | CommandCompleted -> // #I
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
            |> cmd |> ReaderWriterMsg.Command)

    member this.Read(createReadRequest)   = postAndAsyncReply Read  createReadRequest
    member this.Write(createWriteRequest) = postAndAsyncReply Write createWriteRequest



