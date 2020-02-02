open System
open Tweetinvi.Models;
open System.Reactive.Linq
open System.Reactive.Concurrency
open System.Reactive
open System.Threading.Tasks
open TwitterEmotionAnalysis.FSharp
open TwitterEmotionAnalysis

[<EntryPoint>]
let main argv =
    
    let colorEmotion (emotion: Emotion) =
        match emotion with
        | Unhappy -> System.ConsoleColor.Red
        | Indifferent -> System.ConsoleColor.Cyan
        | Happy -> System.ConsoleColor.Green
        | Unknown -> System.ConsoleColor.Gray
        
    let print (text : string) color =
        let bakColor = Console.ForegroundColor
        Console.ForegroundColor <- color        
        Console.WriteLine(text)
        Console.ForegroundColor <- bakColor;

    let obs =  TwitterDataProducer.TweetStream.GetReactiveTweets(TimeSpan.FromMilliseconds(150.))
                    .SubscribeOn(TaskPoolScheduler.Default)
               |> Observable.map(fun m -> scoreSentiment m.Text, m)

    let obs =
        TwitterEmotionAnalysis.tweetEmotionObservable (TimeSpan.FromMilliseconds(150.))
        |> Observable.subscribeOn TaskPoolScheduler.Default
        |> Observable.map(fun m -> scoreSentiment m.Tweet.Text, m)
                        
    let disposable = obs.Subscribe(fun (m, t) ->
        colorEmotion m |> print (sprintf "%O - %s" m t.Tweet.Text))
    
    Console.WriteLine("Press `Enter` to exit.")
    Console.WriteLine("======================")
    Console.ReadLine() |> ignore
    0
