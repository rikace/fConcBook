module MapReduce.Task

open Paket

// Listing 5.14 PageRank object encapsulates the Map and Reduce functions
type PageRank (ranks:seq<string*float>) =
    let map = Map.ofSeq ranks //#A
    let getRank package =
        match map.TryFind package with //#B
        | Some(rank) -> rank
        | None -> 1.0

    member this.Map (package:NuGet.NuGetPackageCache) =
        let score = //#C
            (getRank package.PackageName) / float(package.Dependencies.Length)
        package.Dependencies //#C
        |> Seq.map (fun (Domain.PackageName(name,_),_,_) -> (name, score))

    member this.Reduce (name:string) (values:seq<float>) =
        name, Seq.sum values //#D