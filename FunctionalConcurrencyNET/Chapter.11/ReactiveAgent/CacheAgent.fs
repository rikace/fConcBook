module CacheAgent

open System
open System.Collections.Generic
open SimpleAgents

//Listing 11.7 A Cache agent using the MailboxProcessor
type CacheMessage<'Key> =
    | GetOrSet of 'Key * AsyncReplyChannel<obj>
    | UpdateFactory of Func<'Key,obj>
    | Clear    // #A

type Cache<'Key when 'Key : comparison>
        (factory : Func<'Key, obj>,  ?timeToLive : int) =    // #B
    let timeToLive = defaultArg timeToLive 1000
    let expiry = TimeSpan.FromMilliseconds (float timeToLive)    // #C

    let cacheAgent =
        Agent.Start(fun inbox ->
            let cache = Dictionary<'Key, (obj * DateTime)>( HashIdentity.Structural)    // #D
            let rec loop (factory:Func<'Key, obj>) = async {
                let! msg = inbox.TryReceive timeToLive    // #E
                match msg with
                | Some (GetOrSet (key, channel)) ->
                    match cache.TryGetValue(key) with    // #F
                    | true, (v,dt) when DateTime.Now - dt < expiry -> // #F
                        channel.Reply v
                        return! loop factory
                    | _ ->
                        let value = factory.Invoke(key)    // #F
                        channel.Reply value
                        cache.Add(key, (value, DateTime.Now))
                        return! loop factory
                | Some(UpdateFactory newFactory) ->    // #G
                    return! loop (newFactory)
                | Some(Clear) ->
                    cache.Clear()
                    return! loop factory
                | None ->
                    cache
                    |> Seq.filter(function KeyValue(k,(_, dt)) ->
                                            DateTime.Now - dt > expiry)
                    |> Seq.iter(function KeyValue(k, _) ->
                                            cache.Remove(k) |> ignore)
                    return! loop factory
                }
            loop factory )
    member this.TryGet<'a>(key : 'Key) = async {
        let! item = cacheAgent.PostAndAsyncReply(    // #H
                            fun channel -> GetOrSet(key, channel))
        match item with
        | :? 'a as v -> return Some v
        | _ -> return None  }

    member this.GetOrSetTask (key : 'Key) =
        cacheAgent.PostAndAsyncReply(fun channel -> GetOrSet(key, channel))
        |> Async.StartAsTask  // #I

    member this.UpdateFactory(factory:Func<'Key, obj>) =
        cacheAgent.Post(UpdateFactory(factory))    // #G

    member this.Clear() = cacheAgent.Post(Clear)

open System.Threading

//Listing 11.8 Cache with event notification for refreshed items
type CacheNotification<'Key when 'Key : comparison>
        (factory : Func<'Key, obj>,  ?timeToLive : int, ?synchContext:SynchronizationContext) =

    let timeToLive = defaultArg timeToLive 1000
    let expiry = TimeSpan.FromMilliseconds (float timeToLive)

    let cacheItemRefreshed = Event<('Key * 'obj)[]>()  //#A

    let reportBatch items =    // #B
        match synchContext with
        | None -> cacheItemRefreshed.Trigger(items)  // #C
        | Some ctx ->
           ctx.Post((fun _ -> cacheItemRefreshed.Trigger(items)), null) // #D

    let cacheAgent =
       Agent.Start(fun inbox ->
        let cache = Dictionary<'Key, (obj * DateTime)>(HashIdentity.Structural)
        let rec loop (factory:Func<'Key, obj>) = async {
            let! msg = inbox.TryReceive timeToLive
            match msg with
            | Some (GetOrSet (key, channel)) ->
                match cache.TryGetValue(key) with
                | true, (v,dt) when DateTime.Now - dt < expiry ->
                    channel.Reply v
                    return! loop factory
                | _ ->
                    let value = factory.Invoke(key)
                    channel.Reply value
                    reportBatch ([| (key, value) |])    // #E
                    cache.Add(key, (value, DateTime.Now))
                    return! loop factory
            | Some(UpdateFactory newFactory) ->
                return! loop (newFactory)
            | Some(Clear) ->
                cache.Clear()
                return! loop factory
            | None ->
                cache
                |> Seq.choose(
                    function KeyValue(k,(_, dt)) ->
                                if DateTime.Now - dt > expiry then
                                    let value, dt = factory.Invoke(k), DateTime.Now
                                    cache.[k] <- (value,dt)
                                    Some (k, value)
                                else None)
                |> Seq.toArray
                |> reportBatch    // #E
                return! loop factory
            }
        loop factory )

    member this.TryGet<'a>(key : 'Key) = async {
        let! item = cacheAgent.PostAndAsyncReply(
                        fun channel -> GetOrSet(key, channel))
        match item with
        | :? 'a as v -> return Some v
        | _ -> return None  }
    member this.DataRefreshed = cacheItemRefreshed.Publish  // #A
    member this.Clear() = cacheAgent.Post(Clear)