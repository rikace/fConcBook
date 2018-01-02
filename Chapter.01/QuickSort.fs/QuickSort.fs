module QuickSort

open System
open System.Threading.Tasks

type ParallelismHelpers =
    static member MaxDepth =
        int (Math.Log(float Environment.ProcessorCount, 2.0))

    static member TotalWorkers =
        int (2.0 ** float (ParallelismHelpers.MaxDepth))


// Listing 1.1 A simple Quick-sort Algorithm
let rec quicksortSequential aList =
    match aList with
    | [] -> []
    | firstElement :: restOfList ->
        let smaller, larger =
            List.partition (fun number -> number > firstElement) restOfList
        quicksortSequential smaller @ (firstElement :: quicksortSequential larger)


// Listing 1.2 A parallel Quick-Sort Algorithm using the TPL library
let rec quicksortParallel aList =
    match aList with
    | [] -> []
    | firstElement :: restOfList ->
        let smaller, larger =
            List.partition (fun number -> number > firstElement) restOfList
        let left  = Task.Run(fun () -> quicksortParallel smaller) // #A
        let right = Task.Run(fun () -> quicksortParallel larger)  // #A
        left.Result @ (firstElement :: right.Result)              // #B


// Listing 1.3 A better parallel Quick-Sort Algorithm using the TPL library
let rec quicksortParallelWithDepth depth aList =    // #A
    match aList with
    | [] -> []
    | firstElement :: restOfList ->
        let smaller, larger =
            List.partition (fun number -> number > firstElement) restOfList
        if depth < 0 then   // #B
            let left  = quicksortParallelWithDepth depth smaller  //#C
            let right = quicksortParallelWithDepth depth larger   //#C
            left @ (firstElement :: right)
        else
            let left  = Task.Run(fun () -> quicksortParallelWithDepth (depth - 1) smaller) // #D
            let right = Task.Run(fun () -> quicksortParallelWithDepth (depth - 1) larger)  // #D
            left.Result @ (firstElement :: right.Result)
