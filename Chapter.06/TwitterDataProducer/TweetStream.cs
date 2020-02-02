using System;
using System.IO;
using System.Reactive.Linq;
using Newtonsoft.Json.Linq;
using Tweetinvi.Models;

namespace TwitterDataProducer
{
    public static class TweetStream
    {
        public static IObservable<ITweet> GetReactiveTweets(TimeSpan throttle, string filePath = @"./Data/tweets.txt")
        {
            var tweetFilePath = filePath;
            return Observable.Using(
                () => new StreamReader(tweetFilePath),
                reader => Observable.FromAsync(reader.ReadLineAsync)
                    .Throttle(throttle)
                    .TakeWhile(line => line != null)
                    .Repeat()
                    .Select(line =>
                    {
                        var json = JObject.Parse(line);
                        return new Tweet(json["TweetDTO"]);
                    }));
        }
    }
}