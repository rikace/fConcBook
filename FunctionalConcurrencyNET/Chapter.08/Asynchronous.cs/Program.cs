using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            //Listing 8.8 Cancellation Token callback
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            Task.Run(async () =>
            {
                var webClient = new WebClient();
                token.Register(() => webClient.CancelAsync());  // #A

                var data = await
                    webClient.DownloadDataTaskAsync("http://www.manning.com");
            }, token);

            tokenSource.Cancel();
        }

        void CooperativeCancellation()
        {
            //Listing 8.9 Cooperative cancellation token
            CancellationTokenSource ctsOne = new CancellationTokenSource(); // #A
            CancellationTokenSource ctsTwo = new CancellationTokenSource(); // #A
            CancellationTokenSource ctsComposite = CancellationTokenSource.CreateLinkedTokenSource(ctsOne.Token, ctsTwo.Token); // #B

            CancellationToken ctsCompositeToken = ctsComposite.Token;
            Task.Factory.StartNew(async () =>
            {
                var webClient = new WebClient();
                ctsCompositeToken.Register(() => webClient.CancelAsync());

                await webClient.DownloadDataTaskAsync("http://www.manning.com");
            }, ctsComposite.Token);	// #C
        }
    }
}
