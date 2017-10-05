module RxIScheduler

open System
open System.Reactive.Concurrency
open FSharpx.Collections
open System.Reactive.Disposables

// Part of IScheduler contract
type ScheduledAction<'a> = Func<IScheduler, 'a, IDisposable>
// Action with pre-filled parameters for delayed execution
// IScheduler should be able to schedule any type of work
// So agent message contract should be agnostic against 'a
type ScheduledAction = Func<IDisposable>

// The action+due with custom IComparable for FSharpx.Collections.IPriorityQueue
type ScheduleRequest(due:DateTimeOffset, action:ScheduledAction) =
    member this.Due = due
    member this.Action = action
    member val IsCanceled = false with get, set

    interface IComparable<ScheduleRequest> with
        member this.CompareTo other =
            due.CompareTo other.Due
    interface IComparable with
        member this.CompareTo obj =
            match obj with
            | null  -> 1
            | :? ScheduleRequest as other ->
                (this :> IComparable<_>).CompareTo other
            | _     -> invalidArg "obj" "not a ScheduleRequest<'a>"
    interface IEquatable<ScheduleRequest> with
        member this.Equals other =
            due = other.Due && action = other.Action
    override this.Equals obj =
        match obj with
        | :? ScheduleRequest as other ->
            (this :> IEquatable<_>).Equals other
        | _ -> false
    override this.GetHashCode () =
        due.GetHashCode ()

// Agent message contract
type ScheduleMessage = ScheduleRequest * AsyncReplyChannel<IDisposable>

let schedulerAgent (inbox:MailboxProcessor<ScheduleMessage>) =
    let rec execute (queue:IPriorityQueue<ScheduleRequest>) =  async {
        match queue |> PriorityQueue.tryPop with
        | None -> return! idle queue -1
        | Some(req, tail) ->
            let timeout = int <| (req.Due - DateTimeOffset.Now).TotalMilliseconds
            if timeout > 0 && (not req.IsCanceled)
            then return! idle queue timeout
            else
                if not req.IsCanceled
                    then req.Action.Invoke() |> ignore // TODO: Why IDisposable here?
                return! execute tail
        }
    and idle (queue:IPriorityQueue<_>) timeout = async {
        let! msg = inbox.TryReceive(timeout)
        let queue' =
            match msg with
            | None -> queue
            | Some(request, replyChannel)->
                replyChannel.Reply(
                    Disposable.Create(fun () -> request.IsCanceled <- true)
                )
                queue |> PriorityQueue.insert request
        return! execute queue'
        }
    idle (PriorityQueue.empty(false)) -1


type ParallelAgentScheduler(workers:int) =
    let agent = MailboxProcessor<ScheduleMessage>
                    .parallelWorker(workers, schedulerAgent)

    interface IScheduler with
        member this.Schedule(state:'a, due:DateTimeOffset, action:ScheduledAction<'a>) =
            agent.PostAndReply(fun repl ->
                let action () = action.Invoke(this :> IScheduler, state)
                let req = ScheduleRequest(due, Func<_>(action))
                req, repl)

        member this.Now = DateTimeOffset.Now
        member this.Schedule(state:'a, action) =
            let scheduler = this :> IScheduler
            let due = scheduler.Now
            scheduler.Schedule(state, due, action)
        member this.Schedule(state:'a, due:TimeSpan, action:ScheduledAction<'a>) =
            let scheduler = this :> IScheduler
            let due = scheduler.Now.Add(due)
            scheduler.Schedule(state, due, action)
    static member Create(workers:int) =  ParallelAgentScheduler(workers) :> IScheduler
