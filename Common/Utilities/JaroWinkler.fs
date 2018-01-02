namespace FuzzyMatch

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix); RequireQualifiedAccess>]
module JaroWinkler =

    [<AutoOpen>]
    module FuzyMatchStructures =

        [<StructAttribute>]
        type WordDistanceStruct(word : string, distance : float) =
            member this.Word = word
            member this.Distance = distance

    open System
    open System.Threading
    open System.Threading.Tasks
    open System.Collections.Generic


    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix); RequireQualifiedAccess>]
    module private Array =
        let inline last (array:_[]) = array.[array.Length - 1]

        let inline tryLast (array:_[]) =
            match array with
            | null | [||] -> None
            | [|x|] -> Some(x)
            | a -> Some(a.[a.Length - 1])

        let inline tryLastOption (array:_[] option) =
            match array with
            | Some([|x|]) -> Some(x)
            | Some(a) -> Some(a.[a.Length - 1])
            | _ -> None

        let inline trySortBy sort (array:_[]) =
            match array with
            | null | [||] -> None
            | [|x|] -> Some([|x|])
            | n -> Some(array |> Array.sortBy sort)


        [<RequireQualifiedAccess>]
        module Parallel =
            let filter predicate array =
                array |> Array.Parallel.choose (fun x -> if predicate x then Some x else None)

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix); RequireQualifiedAccess>]
    module private FuzzyMatch =

        let inline score str1Len str2Len distance =
            let len = max str1Len str2Len |> float
            1.0 - (float distance / len)

        let memoizeConcurrent f =
            let dict = System.Collections.Concurrent.ConcurrentDictionary<_, _>(HashIdentity.Structural)
            fun x -> dict.GetOrAdd(x, lazy (f x)).Force()

        let memoize f =
            let dict = new System.Collections.Generic.Dictionary<_, _>(HashIdentity.Structural)
            fun n ->
                match dict.TryGetValue(n) with
                | (true, v) -> v
                | _ ->
                    let temp = f (n)
                    dict.Add(n, temp)
                    temp

    /// Given an offset and a radius from that office, does mChar exist in that part of str?
    let inline private existsInWindow (mChar : char) (str : string) offset radius =
        let startAt = max 0 (offset - radius)
        let endAt = min (offset + radius) (String.length str - 1)
        if endAt - startAt < 0 then false
        else
            let rec exists index =
                if str.[index] = mChar then true
                elif index = endAt then false
                else exists (index + 1)
            exists startAt

    let inline private jaro s1 s2 =
        // The radius is the half lesser of the two string lengths rounded up.
        let matchRadius =
            let minLen = min (String.length s1) (String.length s2)
            minLen / 2 + minLen % 2

        // An inner function which recursively finds the number of matched characters
        // within the radius.
        let commonChars (chars1 : string) (chars2 : string) =
            let rec inner i result =
                match i with
                | -1 -> result
                | _ ->
                    if existsInWindow chars1.[i] chars2 i matchRadius then inner (i - 1) (chars1.[i] :: result)
                    else inner (i - 1) result
            inner (chars1.Length - 1) []

        // The sets of common characters and their lengths as floats
        let c1 = commonChars s1 s2
        let c2 = commonChars s2 s1
        let c1length = float (List.length c1)
        let c2length = float (List.length c2)

        // The number of transpositions within the sets of common characters
        let transpositions =
            let rec inner cl1 cl2 result =
                match cl1, cl2 with
                | [], _ | _, [] -> result
                | c1h :: c1t, c2h :: c2t ->
                    if c1h <> c2h then inner c1t c2t (result + 1.0)
                    else inner c1t c2t result

            let mismatches = inner c1 c2 0.0
            (// If one common string is longer than the other
                // each additional char counts as half a transposition
                mismatches + abs (c1length - c2length)) / 2.0

        let s1length = float (String.length s1)
        let s2length = float (String.length s2)
        let tLength = max c1length c2length
        // The jaro distance as given by 1/3 ( m2/|s1| + m1/|s2| + (mc-t)/mc )
        let result = (c1length / s1length + c2length / s2length + (tLength - transpositions) / tLength) / 3.0
        // This is for cases where |s1|, |s2| or m are zero
        if Double.IsNaN result then 0.0
        else result

    let distance (s1 : string) (s2 : string) =
        // Optimizations for easy to calculate cases
        if s1.Length = 0 || s2.Length = 0 then 0.0
        elif s1 = s2 then 1.0
        else
            // Even more weight for the first char
            let score =
                let jaroScore = jaro s1 s2
                // Accumulate the number of matching initial characters
                let maxLength = (min s1.Length s2.Length) - 1

                let rec calcL i acc =
                    if i > maxLength || s1.[i] <> s2.[i] then acc
                    else calcL (i + 1) (acc + 1.0)

                let l = min (calcL 0 0.0) 4.0
                // Calculate the JW distance
                let p = 0.1
                jaroScore + (l * p * (1.0 - jaroScore))

            let p = 0.2 //percentage of score from new metric

            let b =
                if s1.[0] = s2.[0] then 1.0
                else 0.0
            ((1.0 - p) * score) + (p * b)

    [<CompiledNameAttribute("Match")>]
    let getMatch w input =
         WordDistanceStruct(w, distance w input)

    let inline private findMatches (words : HashSet<string>) (input : string) =
        words
        |> Seq.filter(fun w -> abs (w.Length - input.Length) <= 3)
        |> Seq.map (fun w -> WordDistanceStruct(w, distance w input))
        |> Seq.sortByDescending(fun x -> x.Distance)

    let bestMatches (words : HashSet<string>) (input : string) =
        findMatches words input
        |> Seq.take 5

    //[<CompiledNameAttribute("Match")>]
    let bestMatch (words : HashSet<string>) (input : string) =
        findMatches words input
        |> Seq.head

    module Parallel =
        //[<CompiledNameAttribute("Match")>]
        let bestMatch (words : string []) (input : string) =
            let result =
                words
                |> Array.Parallel.filter(fun w -> abs (w.Length - input.Length) <= 3)
                |> Array.Parallel.map (fun w -> WordDistanceStruct(w, distance w input))
                |> Array.sortByDescending(fun x -> x.Distance)
            result |> Array.take 5

