module WeakMemoization

open System
open System.Collections.Concurrent
open System.Runtime.CompilerServices


let memoizeWeakWithTtl (func :'a->'b) ttl =
    let keyStore = ConcurrentDictionary<int, 'a>()
    let reduceKey (obj: 'a) =
        let oldObj = keyStore.GetOrAdd(obj.GetHashCode(), obj)
        if obj.Equals(oldObj) then oldObj else obj

    let cache = ConditionalWeakTable<'a, 'b * DateTime>()
    let factoryFunc =
        ConditionalWeakTable<'a, 'b * DateTime>
            .CreateValueCallback(
                fun key -> func key, DateTime.Now + ttl)

    fun (arg:'a) ->
        let key = reduceKey arg

        let (value, due) = cache.GetValue(key, factoryFunc)
        if (due < DateTime.Now)
        then
            let newValue = factoryFunc.Invoke(key)
            cache.Remove(key) |> ignore
            cache.Add(key, newValue)
            fst newValue
        else value


let example() =
    let greating name =
        sprintf "Warm greetings %s, the time is %s"
            name (DateTime.Now.ToString("hh:mm:ss"))

    let greetingMemoize =
        memoizeWeakWithTtl greating (TimeSpan.FromSeconds(2.0))

    printfn "%s" <| greetingMemoize("Richard")
    System.Threading.Thread.Sleep(1500)
    printfn "%s" <| greetingMemoize("_Richard".Substring(1))
    System.Threading.Thread.Sleep(1500)
    printfn "%s" <| greetingMemoize("_Richard".Substring(1))

