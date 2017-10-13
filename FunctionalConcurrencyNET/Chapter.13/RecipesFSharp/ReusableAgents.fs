module ReusableAgents

open System
open System.Collections.Generic
open System.Threading

type Agent<'T> = MailboxProcessor<'T>

/// Agent that can be used to implement batch processing. It creates groups
/// of messages (added using the Enqueue method) and emits them using the 
/// BatchProduced event. A group is produced when it reaches the maximal 
/// size or after the timeout elapses.
type BatchProcessingAgent<'T>(bulkSize, timeout) = 

  let bulkEvent = new Event<'T[]>()
  let agent : Agent<'T> = Agent.Start(fun agent -> 
    let rec loop remainingTime messages = async {
      let start = DateTime.Now
      let! msg = agent.TryReceive(timeout = max 0 remainingTime)
      let elapsed = int (DateTime.Now - start).TotalMilliseconds
      match msg with 
      | Some(msg) when 
            List.length messages = bulkSize - 1 ->
          bulkEvent.Trigger(msg :: messages |> List.rev |> Array.ofList)
          return! loop timeout []
      | Some(msg) ->
          return! loop (remainingTime - elapsed) (msg::messages)
      | None when List.length messages <> 0 -> 
          bulkEvent.Trigger(messages |> List.rev |> Array.ofList)
          return! loop timeout []
      | None -> 
          return! loop timeout [] }
    loop timeout [] )

  /// The event is triggered when a group of messages is collected. The
  /// group is not empty, but may not be of the specified maximal size
  /// (when the timeout elapses before enough messages is collected)
  [<CLIEvent>]
  member x.BatchProduced = bulkEvent.Publish

  /// Sends new message to the agent
  member x.Enqueue v = agent.Post(v)
    


    
[<AutoOpenAttribute>]
module AsyncQueue =

    // represent a queue operation
    type Instruction<'T> =
        | Enqueue of 'T * (unit -> unit) 
        | Dequeue of ('T -> unit)

    type AsyncBoundedQueue<'T> (capacity: int, ?cancellationToken:CancellationTokenSource) =
        let waitingConsumers, elts, waitingProducers = Queue(), Queue<'T>(), Queue()
        let cancellationToken = defaultArg cancellationToken (new CancellationTokenSource())

(*  The following balance function shuffles as many elements through the queue 
    as possible by dequeuing if there are elements queued and consumers waiting 
    for them and enqueuing if there is capacity spare and producers waiting *)
        let rec balance() =
            if elts.Count > 0 && waitingConsumers.Count > 0 then
                elts.Dequeue() |> waitingConsumers.Dequeue()
                balance()
            elif elts.Count < capacity && waitingProducers.Count > 0 then
                let x, reply = waitingProducers.Dequeue()
                reply()
                elts.Enqueue x
                balance()

(*  This agent sits in an infinite loop waiting to receive enqueue and dequeue instructions, 
    each of which are queued internally before the internal queues are rebalanced *)
        let agent = MailboxProcessor.Start((fun inbox ->
                let rec loop() = async { 
                        let! msg = inbox.Receive()
                        match msg with
                        | Enqueue(x, reply) -> waitingProducers.Enqueue (x, reply)
                        | Dequeue reply -> waitingConsumers.Enqueue reply
                        balance()
                        return! loop() }
                loop()), cancellationToken.Token)

        member __.AsyncEnqueue x =
              agent.PostAndAsyncReply (fun reply -> Enqueue(x, reply.Reply))
        member __.AsyncDequeue() =
              agent.PostAndAsyncReply (fun reply -> Dequeue reply.Reply)

        interface System.IDisposable with          
              member __.Dispose() = 
                cancellationToken.Cancel()
                (agent :> System.IDisposable).Dispose()

module BlockingAgent =

    /// Type of messages internally used by 'BlockingQueueAgent<T>'
    type internal BlockingAgentMessage<'T> = 
      // Send item to the queue and block until it is added 
      | Add of 'T * AsyncReplyChannel<unit> 
      // Get item from the queue (block if no item available)
      | Get of AsyncReplyChannel<'T>


    /// <summary> Agent that implements an asynchronous blocking queue. </summary>
    /// <remarks>
    ///   The queue has maximal length (maxLength) and if the queue is 
    ///   full, adding to it will (asynchronously) block the caller. When
    ///   the queue is empty, the caller will be (asynchronously) blocked
    ///   unitl an item is available.
    /// </remarks>
    type BlockingQueueAgent<'T>(maxLength) =
      do 
        if maxLength <= 0 then 
          invalidArg "maxLenght" "Maximal length of the queue should be positive."

      // We keep the number of elements in the queue in a local field to
      // make it immediately available (so that 'Count' property doesn't 
      // have to use messages - which would be a bit slower)
      [<VolatileField>]
      let mutable count = 0

      let agent = Agent.Start(fun agent ->
        // Keeps a list of items that are in the queue
        let queue = new Queue<_>()
        // Keeps a list of blocked callers and additional values
        let pending = new Queue<_>()

        // If the queue is empty, we cannot handle 'Get' message
        let rec emptyQueue() = 
          agent.Scan(fun msg ->
            match msg with 
            | Add(value, reply) -> Some <| async {
                queue.Enqueue(value)
                count <- queue.Count
                reply.Reply()
                return! nonEmptyQueue() }
            | _ -> None )

        // If the queue is non-empty, we can handle all messages
        and nonEmptyQueue() = async {
          let! msg = agent.Receive()
          match msg with 
          | Add(value, reply) -> 
              // If the queue has space, we add item and notif caller back;
              // if it is full, we enqueue the request (and block caller)
              if queue.Count < maxLength then 
                queue.Enqueue(value)
                count <- queue.Count
                reply.Reply()
              else 
                pending.Enqueue(value, reply) 
              return! nonEmptyQueue()
          | Get(reply) -> 
              let item = queue.Dequeue()
              // We took item from the queue - check if there are any blocked callers
              // and values that were not added to the queue because it was full
              while queue.Count < maxLength && pending.Count > 0 do
                let itm, caller = pending.Dequeue()
                queue.Enqueue(itm)
                caller.Reply()
              count <- queue.Count
              reply.Reply(item)
              // If the queue is empty then switch the state, otherwise loop
              if queue.Count = 0 then return! emptyQueue()
              else return! nonEmptyQueue() }

        // Start with an empty queue
        emptyQueue() )

      /// Returns the number of items in the queue (immediately)
      /// (excluding items that are being added by callers that have been
      /// blocked because the queue was full)
      member x.Count = count

      /// Asynchronously adds item to the queue. The operation ends when
      /// there is a place for the item. If the queue is full, the operation
      /// will block until some items are removed.
      member x.AsyncAdd(v:'T, ?timeout) = 
        agent.PostAndAsyncReply((fun ch -> Add(v, ch)), ?timeout=timeout)

      /// Asynchronously gets item from the queue. If there are no items
      /// in the queue, the operation will block unitl items are added.
      member x.AsyncGet(?timeout) = 
        agent.PostAndAsyncReply(Get, ?timeout=timeout)


module ThrottlingAgnet =

    /// Message type used by the agent - contains queueing 
    /// of work items and notification of completion 
    type internal ThrottlingAgentMessage<'a, 'b>= 
      | Completed of 'b
      | Get of AsyncReplyChannel<'b>
      | Work of 'a
    
    /// Represents an agent that runs operations in concurrently. When the number
    /// of concurrent operations exceeds 'limit', they are queued and processed later
    type ThrottlingAgent<'a, 'b>(f:'a -> 'b, limit) = 
      let agent = MailboxProcessor.Start(fun agent -> 

        let queue = Queue<_>()
        /// Represents a state when the agent is blocked
        let rec waiting () = 
          // Use 'Scan' to wait for completion of some work
          agent.Scan(function
            | Completed(b) ->   queue.Enqueue b
                                Some(working (limit - 1))
            | _ -> None)

        /// Represents a state when the agent is working
        and working count = async { 
          // Receive any message 
          let! msg = agent.Receive()
          match msg with 
          | Completed(b) ->
              queue.Enqueue b 
              // Decrement the counter of work items
              return! working (count - 1)
          | Work work ->
              // Start the work item & continue in blocked/working state
              async {   let res = f work // TODO CATCH ERROR
                        agent.Post(Completed(res)) }
              |> Async.Start
              if count < limit - 1 then return! working (count + 1)
              else return! waiting () }

        // Start in working state with zero running work items
        working 0)      

      /// Queue the specified asynchronous workflow for processing
      member x.DoWork(work) = agent.Post(Work work)


module BatchProcessor =
    
    /// Agent that implements batch processing
    type BatchProcessor<'T>(count, ?eventContext:SynchronizationContext) =
    
      let batchEvent = new Event<'T[]>()
      let reportBatch batch =
        match eventContext with 
        | None -> 
            batchEvent.Trigger(batch)
        | Some ctx ->            
            ctx.Post((fun _ -> batchEvent.Trigger(batch)), null)

      let agent = Agent<'T>.Start(fun inbox -> async {
        while true do
          let queue = new ResizeArray<_>()
          for i in 1 .. count do
            let! msg = inbox.Receive()
            queue.Add(msg)
          reportBatch (queue.ToArray()) })

      member x.BatchProduced = batchEvent.Publish
      member x.Post(value) = agent.Post(value)