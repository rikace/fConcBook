namespace DataParallelism.Part1.FSharp

module PrimeNumbers =
    open System
    open System.Linq
    open FSharp.Collections.ParallelSeq

    let isPrime n =
        if n < 2 then false
        elif n = 2 then true
        else
            let boundary = int(Math.Floor(Math.Sqrt(float(n))))
            [2..boundary]
            |> Seq.exists (fun i-> (n%i)=0)
            |> not

    let sequentialSum len =
        Seq.init len id
        |> Seq.filter (isPrime)
        |> Seq.sumBy (fun x-> int64(x))

    let parallelSum len =
        Seq.init len id
        |> PSeq.withDegreeOfParallelism(Environment.ProcessorCount)
        |> PSeq.withMergeOptions(ParallelMergeOptions.FullyBuffered)
        |> PSeq.filter (isPrime)
        |> Seq.sumBy (fun x-> int64(x))

    let parallelLinqSum len =
        (Seq.init len id).AsParallel().Where(isPrime).Sum(fun x -> int64(x))