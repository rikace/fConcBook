module MapReduce.Data

open MBrace.FsPickler
open FSharp.Data
open Paket
open System.IO
open System.IO.Compression

type AutoComplete =
    JsonProvider< """{"@context":{"@vocab":"http://schema.nuget.org/schema#"},"totalHits":59717,"lastReopen":"2016-10-05T12:05:37.4829982Z","index":"v3-lucene0-v2v3-20161003","data":["ABCFramework.Wpf","44nwodhterag"]}""" >

let NuGetV2URL = "https://www.nuget.org/api/v2"
let NuGetV3URL = "https://api.nuget.org/v3/index.json"

let getAllPackageIds () =
    let batchSize = 1000
    let searchUrl =
        NuGetV3.getSearchAPI(None, NuGetV3URL)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    let rec getPackages url skip =
      seq {
        let resp =
            sprintf "%s?skip=%d&take=%d"
                url skip batchSize
            |> AutoComplete.Load
        yield! resp.Data
        if resp.Data.Length = batchSize
        then yield! getPackages url (skip+batchSize)
      }
    match searchUrl with
    | Some(url) -> getPackages url 0
    | _ -> Seq.empty

let loadPackageInfo id =
  async {
    printfn "Processing package '%s' ..." id
    let package = Domain.PackageName(id)
    let! versions' =
        NuGetV2.tryGetPackageVersionsViaJson(None, NuGetV2URL, package)
        |> Async.Catch
    printfn "Versions received for '%s'." id
    match versions' with
    | Choice1Of2(Some(versions)) when versions.Length > 0 ->
        let latest = SemVer.Parse(versions.[versions.Length-1])
        let! meta =
            NuGetV2.getDetailsFromNuGetViaODataFast None NuGetV2URL package latest
            |> Async.Catch
        printfn "Done with package '%s'." id
        return
            match meta with
            | Choice1Of2(data) -> Some(data)
            | _ -> None
    | _ -> return None
  }

// Real: 01:59:43.658, CPU: 00:07:29.703, GC gen0: 4728, gen1: 1186, gen2: 47
let loadPackagesFromNuGet () =
    printfn "Downloading NuGet ... (Please wait, it will be long, about '2h')"
    getAllPackageIds()
    |> Seq.map loadPackageInfo
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Array.choose (id)


let modelFile = __SOURCE_DIRECTORY__ + "/nuget-latest-versions.model"

let loadPackages () =
    let serializer = FsPickler.CreateBinarySerializer()
    if File.Exists modelFile then
        use fs = new FileStream(modelFile, FileMode.Open)
        use gs = new GZipStream(fs, CompressionMode.Decompress)
        serializer.DeserializeSequence<NuGet.NuGetPackageCache>(gs)
        |> Seq.toArray
    else
        let model = loadPackagesFromNuGet()
        use fs = new FileStream(modelFile, FileMode.Create)
        use gs = new GZipStream(fs, CompressionLevel.Optimal)
        serializer.SerializeSequence (gs, model) |> ignore
        model