using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Asynchronous.CSharp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Listing 8.8 Cancellation Token callback
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            Task.Run(async () =>
            {
                var webClient = new WebClient();
                token.Register(() => webClient.CancelAsync()); // #A

                var data = await
                    webClient.DownloadDataTaskAsync("http://www.manning.com");
            }, token);

            tokenSource.Cancel();
        }

        private void CooperativeCancellation()
        {
            //Listing 8.9 Cooperative cancellation token
            var ctsOne = new CancellationTokenSource(); // #A
            var ctsTwo = new CancellationTokenSource(); // #A
            var ctsComposite = CancellationTokenSource.CreateLinkedTokenSource(ctsOne.Token, ctsTwo.Token); // #B

            var ctsCompositeToken = ctsComposite.Token;
            Task.Factory.StartNew(async () =>
            {
                var webClient = new WebClient();
                ctsCompositeToken.Register(() => webClient.CancelAsync());

                await webClient.DownloadDataTaskAsync("http://www.manning.com");
            }, ctsComposite.Token); // #C
        }
    }
}