module ParallelDownloadImages

open BTree
open FunctionalTechniques.cs
open System
open System.IO
open System.Threading.Tasks

//Listing 3.22 Parallel recursive divide-and-conquer function
let maxDepth = int <| Math.Log(float System.Environment.ProcessorCount, 2.)+4. //#A

let webSites : Tree<string> =
    WebCrawlerExample.WebCrawler("http://www.foxnews.com")
    |> Seq.fold(fun tree site -> insert site tree ) Empty //#B

let downloadImage (url:string) =
    use client = new System.Net.WebClient()
    let fileName = Path.GetFileName(url)
    client.DownloadFile(url, @"c:\Images\" + fileName)    //#C

let rec parallelDownloadImages tree depth =               //#D
    match tree with
    | _ when depth = maxDepth ->
        tree |> inorder downloadImage |> ignore
    | Node(leaf, left, right) ->
        let taskLeft  = Task.Run(fun() ->
            parallelDownloadImages left (depth + 1))
        let taskRight = Task.Run(fun() ->
            parallelDownloadImages right (depth + 1))
        let taskLeaf  = Task.Run(fun() -> downloadImage leaf)
        Task.WaitAll([|taskLeft;taskRight;taskLeaf|])     //#E
    | Empty -> ()
