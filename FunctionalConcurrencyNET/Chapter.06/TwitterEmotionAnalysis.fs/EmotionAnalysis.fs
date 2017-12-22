module TwitterEmotionAnalysis.Fs

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
open java.util
open edu.stanford.nlp.pipeline
open edu.stanford.nlp.ling
open edu.stanford.nlp.sentiment
open edu.stanford.nlp.neural.rnn
open edu.stanford.nlp.trees

let jarDirectory = // Folder with Stanford NLP models
    IO.Path.Combine(__SOURCE_DIRECTORY__, "models")

// Listing 6.3 Function to evaluate the emotion of a sentence using StanfordNLP library
let properties = Properties()
properties.setProperty("annotators", "tokenize,ssplit,pos,parse,sentiment") |> ignore

IO.Directory.SetCurrentDirectory(jarDirectory)
let stanfordNLP = StanfordCoreNLP(properties) //#A

type Emotion =
    | Unhappy
    | Indifferent
    | Happy // #B

let getEmotionMeaning value =
    match value with
    | 0 | 1 -> Unhappy
    | 2 -> Indifferent
    | 3 | 4 -> Happy //#C

let evaluateEmotion (text:string) =
    let annotation = Annotation(text)
    stanfordNLP.annotate(annotation)

    let emotions =
        let emotionAnnotationClassName = SentimentCoreAnnotations.SentimentAnnotatedTree().getClass()
        let sentences = annotation.get(CoreAnnotations.SentencesAnnotation().getClass()) :?> java.util.ArrayList
        [ for s in sentences ->
            let sentence = s :?> Annotation
            let sentenceTree = sentence.get(emotionAnnotationClassName) :?> Tree
            let emotion = RNNCoreAnnotations.getPredictedClass(sentenceTree)
            getEmotionMeaning emotion]
    (emotions.[0]) //#D



//Listing 6.4 Settings to enable the Twitterinvi library
// Create new Twitter application and copy-paste
// keys and access tokens to module variables
let consumerKey = "<your Key>"
let consumerSecretKey = "<your secret key>"
let accessToken = "<your access token>"
let accessTokenSecret = "<your secreat access token>"

let cred = new TwitterCredentials(consumerKey, consumerSecretKey, accessToken, accessTokenSecret)
let stream = Stream.CreateSampleStream(cred)
stream.FilterLevel <- StreamFilterLevel.Low

let emotionMap =
    [(Unhappy, 0)
     (Indifferent, 0)
     (Happy, 0)] |> Map.ofSeq

// Listing 6.5 Observable pipeline to analyze the tweets
let observableTweets =
    stream.TweetReceived //#A
    |> Observable.throttle(TimeSpan.FromMilliseconds(50.)) //#B
    |> Observable.filter(fun args ->
        args.Tweet.Language = Language.English) //#C
    |> Observable.groupBy(fun args ->
        evaluateEmotion args.Tweet.FullText) //#D
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
        let cred = new TwitterCredentials(consumerKey, consumerSecretKey, accessToken, accessTokenSecret)
        let stream = Stream.CreateSampleStream(cred)
        stream.FilterLevel <- StreamFilterLevel.Low
        stream.StartStreamAsync() |> ignore

        stream.TweetReceived
        |> Observable.throttle(throttle)
        |> Observable.filter(fun args ->
            args.Tweet.Language = Language.English)
        |> Observable.groupBy(fun args ->
            evaluateEmotion args.Tweet.FullText)
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


open FSharp.Charting
open FSharp.Charting.ChartTypes
open System.Windows.Forms


#if INTERACTIVE

LiveChart.Column(observableTweets,Name="Tweet Emotions").ShowChart()
stream.StartStreamAsync()

#else

[<STAThread; EntryPoint>]
let main argv =
    let form = new Form(Text="Tweet Emotions", Width=500, Height=500)
    form.Load.Add <| fun _ ->
        let chart = LiveChart.Column(observableTweets)
        let host = new ChartControl(chart, Dock = DockStyle.Fill)
        form.Controls.Add(host)
        stream.StartStreamAsync() |> ignore
    Application.Run(form)
    0

#endif