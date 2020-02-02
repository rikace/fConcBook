namespace TwitterEmotionAnalysis.FSharp

module TwitterEmotionAnalysis =

    #if INTERACTIVE
    // Right click on `References`
    // Click on `Send References to F# interactive`
    #load "Observable.fs"
    #endif

    open System
    open System.Reactive.Linq
    open Tweetinvi.Models
    open Tweetinvi
    open Tweetinvi.Streaming.Parameters
    open System.Configuration
    
    type Emotion =
        | Unhappy
        | Indifferent
        | Happy // #B
        | Unknown 

    let getEmotionMeaning value =
        match value with
        | 0 | 1 -> Unhappy
        | 2 -> Indifferent
        | 3 | 4 -> Happy //#C
        | _ -> Unknown
        
    let normalize (em:float32) =
         // Happy
         //Val : 4 - max : 406.204900  - min : -189.103600
         //Val : 3 - max : 431.193100  - min : -331.100800            
         // INdifferent 
         // Val : 2 - max : 419.557500  - min : -311.066700            
         // Unhappy
         // Val : 1 - max : 430.839700  - min : -451.206700
         // Val : 0 - max : 238.980100  - min : -141.332000
         if em <= 406.204900f && em >= 306.103600f then 4
         elif em <= 306.103600f && em >= 0.f then 3
         elif em <= 0.f && em >= -221.206700f then 2
         elif em <= -221.206700f && em >= -431.100800f then 1
         else 0
        

    let runPrediction =
         let sentimentModel = MLearning.loadModel "./model.zip"
         MLearning.runPrediction sentimentModel
         
    let scoreSentiment = runPrediction >> MLearning.scorePrediction >> normalize >> getEmotionMeaning//#D


    //Listing 6.4 Settings to enable the Twitterinvi library
    // Create new Twitter application and copy-paste
    // keys and access tokens to module variables
    module Credentials =
        let consumerKey = ConfigurationManager.AppSettings.["ConsumerKey"]
        let consumerSecret = ConfigurationManager.AppSettings.["ConsumerSecret"]
        let accessToken = ConfigurationManager.AppSettings.["AccessToken"]
        let accessTokenSecret = ConfigurationManager.AppSettings.["AccessTokenSecret"]
        
        let twitterCredentials = new TwitterCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret)
        
    let stream = Stream.CreateSampleStream(Credentials.twitterCredentials)
    stream.FilterLevel <- StreamFilterLevel.Low

    let emotionMap =
        [(Unhappy, 0)
         (Indifferent, 0)
         (Happy, 0)
         (Unknown, 0)] |> Map.ofSeq

    // Listing 6.5 Observable pipeline to analyze the tweets
    let observableTweets =
        stream.TweetReceived //#A
        |> Observable.throttle(TimeSpan.FromMilliseconds(50.)) //#B
        |> Observable.filter(fun args ->
            if args.Tweet.Language.HasValue |> not then false
            else args.Tweet.Language.Value = Language.English) //#C
        |> Observable.groupBy(fun args ->
            scoreSentiment args.Tweet.FullText) //#D
        |> Observable.selectMany(fun args ->
            args |> Observable.map(fun i ->
                (args.Key, (max 1 i.Tweet.FavoriteCount)))) //#E
        |> Observable.scan(fun sm (key,count) ->
            match sm |> Map.tryFind key with
            | Some(v) -> sm |> Map.add key (v + count)
            | None    -> sm ) emotionMap //#F
        |> Observable.map(fun sm ->
            let total = sm |> Seq.sumBy(fun v -> v.Value) //#G
            sm |> Seq.map(fun k ->
                let percentageEmotion = ((float k.Value) * 100.) / (float total)
                let labelText = sprintf "%A - %.2f.%%" (k.Key) percentageEmotion
                (labelText, percentageEmotion)
            ))


    //Listing 6.9 Struct TweetEmotino to maintain the tweet details
    [<Struct>]
    type TweetEmotion(tweet:ITweet, emotion:Emotion) =
        member this.Tweet with get() = tweet
        member this.Emotion with get() = emotion

        static member Create tweet emotion =
            TweetEmotion(tweet, emotion)

    //Listing 6.7 Implementation of Observable Tweet-Emotions
    let tweetEmotionObservable(throttle:TimeSpan) =
        Observable.Create(fun (observer:IObserver<_>) -> //#A            
            let cred = Credentials.twitterCredentials
            let stream = Stream.CreateSampleStream(cred)
            stream.FilterLevel <- StreamFilterLevel.Low
            stream.StartStreamAsync() |> ignore

            stream.TweetReceived
            |> Observable.throttle(throttle)
            |> Observable.filter(fun args ->
                if args.Tweet.Language.HasValue |> not then false
                else args.Tweet.Language.Value = Language.English)
            |> Observable.groupBy(fun args ->
                scoreSentiment args.Tweet.FullText)
            |> Observable.selectMany(fun args ->
                args |> Observable.map(fun tw -> TweetEmotion.Create tw.Tweet args.Key))
            |> Observable.subscribe(observer.OnNext) //#B
        )

    let printUnhappyTweets() =
        { new IObserver<TweetEmotion> with
            member this.OnNext(tweet) =
                if tweet.Emotion = Unhappy then
                    Console.WriteLine(tweet.Tweet.Text)

            member this.OnCompleted() = ()
            member this.OnError(exn) = () }
