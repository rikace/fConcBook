module Partitioner

    open System
    open System.Collections.Generic
    open System.Collections.Concurrent

    type ParallelismHelpers =
        static member TotalWorkers = int (2.0 ** float (int (Math.Log(float Environment.ProcessorCount, 2.0))))

        static member GetPartitions (s : int, e : int) =
            let toSeq (enum : IEnumerator<_>)=
                seq {
                    while enum.MoveNext() do
                        yield enum.Current
                }
            let partitioner = Partitioner.Create(s, e)
            let partitions = partitioner.GetPartitions(ParallelismHelpers.TotalWorkers) |> Seq.collect toSeq |> Seq.toArray
            partitions

    let splitByPartitionCountRange (partitions : int) (startRange : int64) (endRange : int64) : (int64 * int64) [] =
        if startRange > endRange then raise <| new ArgumentOutOfRangeException()
        elif partitions < 1 then invalidArg "partitions" "invalid number of partitions."
        else

        let length = endRange - startRange
        if length = 0L then Array.init partitions (fun _ -> (startRange + 1L, startRange)) else

        let partitions = int64 partitions
        let chunkSize = length / partitions
        let r = length % partitions
        let ranges = new ResizeArray<int64 * int64>()
        let mutable i = startRange
        for p in 0L .. partitions - 1L do
            // add a padding element for every chunk 0 <= p < r
            let j = i + chunkSize + if p < r then 1L else 0L
            let range = (i, j - 1L)
            ranges.Add range
            i <- j

        ranges.ToArray()
