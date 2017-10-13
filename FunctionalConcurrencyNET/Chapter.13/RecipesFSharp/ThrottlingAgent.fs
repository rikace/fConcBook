module AgentThrottlingWithPause 

open System
open System.Collections.Concurrent

// Messages received by the throttling agent
type Message =
    | Work
    | Start of AsyncReplyChannel<unit>
    | Pause
    | Resume
    | Quit

// Throttling agent status
type Status =
    | Idle
    | Paused
    | Working

/// <summary>
/// An agent for throttling the number of concurrently
/// executing asynchronous workflows. The default limit
/// is 5 concurrent tasks. The agent also supports pausing
/// and resuming work.
/// </summary>
type ThrottlingAgent(?Limit) as this =

    let mutable status = Idle

    let limit = defaultArg Limit 5

    // Stores the asynchronous workflows waiting for execution.
    let tasksStack = ConcurrentStack<Async<unit>>()

    // The agent expects to receive a "Start" message first.
    // The message carries a reply channel that is used to
    // notify the caller once all the queued asynchronous
    // workflows are executed.
    [<DefaultValue>] val mutable private replyChannel: AsyncReplyChannel<unit>

    // Attempts to pop and return an asynchronous workflow
    // from the tasks stack.
    let tryPopTask() =
        match tasksStack.TryPop() with
        | false, _ -> None
        | true, workflow -> Some workflow

    let agent =
        MailboxProcessor.Start(fun inbox ->
            let rec loop count =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Pause ->
                        status <- Paused
                        let! _ = inbox.Scan(fun msg ->
                            match msg with
                            | Resume -> Some (async {do ()})
                            | _ -> None
                        )
                        status <- Working 
                        return! loop count
                    | Resume ->
                        return! loop count
                    | Start reply ->
                        status <- Working
                        this.replyChannel <- reply
                        [1 .. limit] |> List.iter (fun _ -> inbox.Post Work)
                        return! loop count
                    | Work ->
                        let workOption = tryPopTask()
                        match workOption with
                        | Some work ->
                            async {
                                try
                                    do! work
                                finally
                                    inbox.Post Work
                            } |> Async.Start
                            return! loop count
                        | None ->
                            inbox.Post Quit
                            return! loop (count + 1)
                    | Quit ->
                        match count with
                        | _ when count = limit ->
                            this.replyChannel.Reply()
                            status <- Idle
                            return! loop 0
                        | _ -> return! loop count
                }
            loop 0
        )

    interface IDisposable with
        member __.Dispose() =
            agent :> IDisposable
            |> fun x -> x.Dispose()

    /// <summary>
    /// Throttles executing the supplied asynchronous workflows.
    /// </summary>
    /// <param name="asyncs">The asynchronous workflows.</param>
    member __.Work asyncs =
        async {
            match status with
            | Idle ->
                tasksStack.PushRange <| Seq.toArray asyncs
                do! agent.PostAndAsyncReply(fun replyChannel -> Start replyChannel)
            | Working -> failwith "The agent is currently working."
            | Paused -> failwith "The agent is currently paused."
        }

    /// Gets the count of the asynchronous workflows waiting
    // for execution.
    member __.RemainingTasks = tasksStack.Count

    /// Pauses the agent.
    member __.Pause() =
        match status with
        | Working -> agent.Post Pause
        | Paused -> failwith "The agent has already been paused."
        | Idle -> failwith "The agent is idle and can't be paused."

    /// Resumes executing the asynchronous workflows.
    member __.Resume() =
        match status with
        | Paused -> agent.Post Resume
        | Working -> failwith "The agent is already working."
        | Idle -> failwith "The agent is idle, there is no work to resume."

    /// Cancels executing the remaining asynchronous workflows.
    member __.CancelRemainingTasks() = tasksStack.Clear()

    /// Gets the agent's status.
    member __.Status = status