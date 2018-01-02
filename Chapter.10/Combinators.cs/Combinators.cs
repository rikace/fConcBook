using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Combinators.cs
{
    public static class Combinators
    {
        //Listing 10.3 Task.Catch function
        public static Task<T> Catch<T, TError>(this Task<T> task, Func<TError, T> onError) where TError : Exception
        {
            var tcs = new TaskCompletionSource<T>();    // #A

            task.ContinueWith(innerTask =>
            {
                if (innerTask.IsFaulted && innerTask?.Exception?.InnerException is TError)
                    tcs.SetResult(onError((TError)innerTask.Exception.InnerException)); // #B
                else if (innerTask.IsCanceled)
                    tcs.SetCanceled();      // #B
                else if (innerTask.IsFaulted)
                    tcs.SetException(innerTask?.Exception?.InnerException ?? throw new InvalidOperationException()); // #B
                else
                    tcs.SetResult(innerTask.Result);  // #B
            });
            return tcs.Task;
        }

        public static async Task CombinatorRedundancy()
        {
            //Listing 10.16 Redundancy with Task.WhenAny
            var cts = new CancellationTokenSource(); // #A

            Func<string, string, string, CancellationToken, Task<string>> GetBestFlightAsync =
                async (from, to, carrier, token) =>
                {
                    string url = $"flight provider {carrier}";
                    using (var client = new HttpClient())
                    {
                        HttpResponseMessage response = await client.GetAsync(url, token);
                        return await response.Content.ReadAsStringAsync();
                    }
                };      // #B

            var recommendationFlights = new List<Task<string>>()
            {
                GetBestFlightAsync("WAS", "SF", "United", cts.Token),
                GetBestFlightAsync("WAS", "SF", "Delta", cts.Token),
                GetBestFlightAsync("WAS", "SF", "AirFrance", cts.Token),
            };  // #C

            Task<string> recommendationFlight = await Task.WhenAny(recommendationFlights);  // #D
            while (recommendationFlights.Count > 0)
            {
                try
                {
                    var recommendedFlight = await recommendationFlight; // #E
                    cts.Cancel();   // #F
                    BuyFlightTicket("WAS", "SF", recommendedFlight);
                    break;
                }
                catch (WebException)    // #E
                {
                    recommendationFlights.Remove(recommendationFlight); // #E
                }
            }
        }

        private static void BuyFlightTicket(string v1, string v2, string recommendedFlight)
            => new NotImplementedException(); // implementation for buying the tickets


        //Listing 10.17 Asynchronous For-Each loop with Task.WhenAll
        static Task ForEachAsync<T>(this IEnumerable<T> source, int maxDegreeOfParallelism, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(maxDegreeOfParallelism)
                select Task.Run(async () =>
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }


        //Listing 10.18 Using the asynchronous For Each loop
        static async Task SendEmailsAsync(List<string> emails)
        {
            SmtpClient client = new SmtpClient();

            Func<string, Task> sendEmailAsync = async emailTo =>
            {
                MailMessage message = new MailMessage("me@me.com", emailTo);
                await client.SendMailAsync(message);
            };

            await emails.ForEachAsync(Environment.ProcessorCount, sendEmailAsync);
        }

    }
}