module FunctionalTechniques

open System
open System.Threading.Tasks
open System.Collections.Generic
open System.Collections.Concurrent
open FSharp.Collections.ParallelSeq
open FuzzyMatch
open System.Linq

// Listing 2.4 F# support for function composition
module Composition =
    let add4 x = x + 4          //#A
    let multiplyBy3 x = x * 3   //#B
    let list = [0..10]          //#C

    let newList  = List.map(fun x -> multiplyBy3(add4(x))) list //#D
    let newList' = list |> List.map(add4 >> multiplyBy3)        //#E


// Listing 2.10 Closure capturing variables in a multithreaded environment in F#
module Closure =
    let tasks = Array.zeroCreate<Task> 10

    for index = 1 to 10 do
        tasks.[index - 1] <- Task.Factory.StartNew(fun () -> Console.WriteLine index)

module Memoization =
    // Listing 2.13 Memoize function in F#
    let memoize func =
        let table = Dictionary<_,_>()
        fun x ->   if table.ContainsKey(x) then table.[x]
                   else
                        let result = func x
                        table.[x] <- result
                        result

    let memoizeThreadSafe (func: 'a -> 'b) =
        let table = ConcurrentDictionary<'a,'b>()
        fun x ->   table.GetOrAdd(x, func)


module ConsurrentSpeculation =

    // Listing 2.24 Fast fuzzy match in F#
    let fuzzyMatch (words:string list) =
        let wordSet = new HashSet<string>(words)    //#A
        let partialFuzzyMatch word =                //#B
            query { for w in wordSet.AsParallel() do
                        select (JaroWinkler.getMatch w word) }
            |> Seq.sortBy(fun x -> -x.Distance)
            |> Seq.head

        fun word -> partialFuzzyMatch word          //#C


    let words = [] // TODO: Fill the list
    let fastFuzzyMatch = fuzzyMatch words         //#D

    let magicFuzzyMatch = fastFuzzyMatch "magic"
    let lightFuzzyMatch = fastFuzzyMatch "light”" //#E


    let fuzzyMatchPSeq (words:string list) =
        let wordSet = new HashSet<string>(words)
        fun word ->
            wordSet
            |> PSeq.map(fun w -> JaroWinkler.bestMatch word w)
            |> PSeq.sortBy(fun x -> -x.Distance)
            |> Seq.head

module Laziness =
    open FunctionalTechniques.cs

    // Listing 2.28 Lazy initialization of the Person object with F#
    let barneyRubble = lazy( Person("barney", "rubble") )  //#A
    printfn "%s" (barneyRubble.Force().FullName)           //#B