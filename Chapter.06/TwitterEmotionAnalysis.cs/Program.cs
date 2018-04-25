using System;
using System.Linq;
using Tweetinvi.Events;
using Tweetinvi.Models;
using Tweetinvi;
using Tweetinvi.Streaming.Parameters;
using Rx = System.Reactive.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive;
using System.Threading.Tasks;
using RxPublisherSubscriber;

namespace TwitterEmotionAnalysis
{
    // Listing 6.10 Implementation of Pub-Sub Tweet-Emotion
    class RxTweetEmotion : RxPubSub<Fs.TweetEmotion>  //#A
    {
        public RxTweetEmotion(TimeSpan throttle) //#B
        {
            var obs = Fs.tweetEmotionObservable(throttle)
                        .SubscribeOn(TaskPoolScheduler.Default); //#C
            base.AddPublisher(obs);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var tweetPositiveObserver = Observer.Create<Fs.TweetEmotion>(tweet =>
            {
                if (tweet.Emotion.IsHappy)
                    Console.WriteLine(tweet.Tweet.Text);
            });


            var rxTweetSentiment = new RxTweetEmotion(TimeSpan.FromMilliseconds(150));
            IDisposable posTweets = rxTweetSentiment.Subscribe(tweetPositiveObserver);

            // uncomment this code to interop with the F# implementation
            // IObserver<Fs.TweetEmotion> unhappyTweetObserver = Fs.printUnhappyTweets();
            // IDisposable disposable = rxTweetSentiment.Subscribe(unhappyTweetObserver);

            Console.WriteLine("Press `Enter` to exit.");
            Console.WriteLine("======================");
            Console.ReadLine();
        }


        static IObservable<Fs.TweetEmotion> TweetEmotionObservable(TimeSpan throttle)
        {
            var consumerKey = "<your Key>";
            var consumerSecretKey = "<your secret key>";
            var accessToken = "<your access token>";
            var accessTokenSecret = "<your secreat access token>";

            //Listing 6.8 Implementation of the TweetSentiment Observable in C#
            return
                Rx.Observable.Create<Fs.TweetEmotion>(observer =>
                {
                    var cred = new TwitterCredentials(
                        consumerKey, consumerSecretKey, accessToken, accessTokenSecret);
                    var stream = Stream.CreateSampleStream(cred);
                    stream.FilterLevel = StreamFilterLevel.Low;
                    stream.StartStreamAsync();

                    return
                      Rx.Observable
                        .FromEventPattern<TweetReceivedEventArgs>(stream, "TweetReceived")
                        .Throttle(throttle)
                        .Select(args => args.EventArgs)
                        .Where(args => args.Tweet.Language == Language.English)
                        .GroupBy(args =>
                            Fs.evaluateEmotion(args.Tweet.FullText))
                        .SelectMany(args =>
                            args.Select(tw => Fs.TweetEmotion.Create(tw.Tweet, args.Key)))
                        .Subscribe(o => observer.OnNext(o));
                });
        }
    }
}
