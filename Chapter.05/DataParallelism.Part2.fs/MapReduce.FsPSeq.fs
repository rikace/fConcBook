namespace MapReduce

module MapReduceFsPSeq =

    open FSharp.Collections.ParallelSeq
    open System.Linq

    // Listing 5.15 Implementation of mapF function for the first phase of the MapReduce pattern
    let mapF  M (map:'in_value -> seq<'out_key * 'out_value>)
                (inputs:seq<'in_value>) =
        inputs
        |> PSeq.withExecutionMode ParallelExecutionMode.ForceParallelism //#A
        |> PSeq.withDegreeOfParallelism M //#B
        |> PSeq.collect (map) //#C
        |> PSeq.groupBy (fst) //#D
        |> PSeq.toList //#E

    // Listing 5.16 Implementation of reduceF function for the second phase of the MapReduce pattern
    let reduceF  R (reduce:'key -> seq<'value> -> 'reducedValues)
                   (inputs:('key * seq<'key * 'value>) seq) =
        inputs
        |> PSeq.withExecutionMode ParallelExecutionMode.ForceParallelism //#A
        |> PSeq.withDegreeOfParallelism R //#B
        |> PSeq.map (fun (key, items) -> //#C
            items
            |> Seq.map (snd) //#D
            |> reduce key)   //#D
        |> PSeq.toList

    // Listing 5.17 Implementation of the MapReduce pattern composing the mapF and reduce functions
    let mapReduce
            (inputs:seq<'in_value>)
            (map:'in_value -> seq<'out_key * 'out_value>)
            (reduce:'out_key -> seq<'out_value> -> 'reducedValues)
            M R =

        inputs |> (mapF M map >> reduceF R reduce) //#A


    //// Code example using (map >> reduce) mapReduce function
    let runMapReduce(ranks:(string * float) seq) =
        let data = Data.loadPackages()

        let executeMapReduce (ranks:(string * float) seq)=
            let M,R = 10,5
            let pg = MapReduce.Task.PageRank(ranks)
            mapReduce data (pg.Map) (pg.Reduce) M R
        executeMapReduce ranks

module MapReduceSequential =

    let mapReduce
            (inputs:seq<'in_value>)
            (map:'in_value -> seq<'out_key * 'out_value>)
            (reduce:'out_key -> seq<'out_value> -> 'reducedValues)
            M R =
        inputs
        |> Seq.collect (map)
        |> Seq.groupBy (fst)
        |> Seq.map (fun (key, items) ->
            items
            |> Seq.map (snd)
            |> reduce key)
        |> Seq.toList
