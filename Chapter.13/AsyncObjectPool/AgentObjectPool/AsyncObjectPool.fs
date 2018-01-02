namespace ParallelRecipes

open System

type PoolMessage<'a> =
    | Get of AsyncReplyChannel<'a>
    | Put of 'a
    | GetCount of AsyncReplyChannel<int>

type AgentObjectPool<'a>(generate: Func<'a>, initialPoolCount) =

    let initial = List.init initialPoolCount (fun _ -> generate.Invoke())
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec loop(objects) = async {
            let! msg = inbox.Receive()
            match msg with
            | Get(reply) ->
                match objects with
                | head :: tail ->
                    reply.Reply(head)
                    return! loop(tail)
                | [] as empty->
                    reply.Reply(generate.Invoke())
                    return! loop(empty)
            | Put(value)->  return! loop(value :: objects)
            | GetCount(reply) ->
                reply.Reply(objects.Length)
                return! loop(objects)
        }
        loop(initial))

    new(generate:unit -> 'a, initialPoolCount) = AgentObjectPool<'a>(Func<'a>(generate), initialPoolCount)

    /// Puts an item into the pool
    member this.PutObject(item) =  agent.Post(Put(item))
    /// Gets an item from the pool or if there are none present use the generator
    member this.GetObject() = agent.PostAndAsyncReply(Get) |> Async.StartAsTask
    /// Gets the current count of items in the pool
    member this.GetAllocationObject() = agent.PostAndAsyncReply(GetCount) |> Async.StartAsTask
