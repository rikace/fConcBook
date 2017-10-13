using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using StockTicker.Core;
using static StockTicker.Core.Models;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace StockTicker.Server.Cs.Controllers
{
    [RoutePrefix("api/trading")]
    public class TradingController : ApiController, IObservable<CommandWrapper>
    {
        public TradingController()
        {
            subject = new Subject<CommandWrapper>();
        }

        private readonly Subject<CommandWrapper> subject;

        private void Publish(string connectionId, Result<TradingRecord> cmd)
        {
            if (cmd.IsOk)
            {
                var wrapped = CommandWrapper.CreateTrading(connectionId, cmd.Ok);
                subject.OnNext(wrapped);
                return;
            }
            subject.OnError(cmd.Error);
        }

        private HttpResponseMessage ToResponse (HttpRequestMessage request, Result<TradingRecord> result)
        {
            if (result.IsOk)
                return request.CreateResponse(HttpStatusCode.OK);
            return request.CreateResponse(HttpStatusCode.BadRequest);
        }

        public HttpResponseMessage ProcessRequest([FromBody]TradingRequest tr, TradingType type)
        {
            var cmd = new TradingRecord(
                tr.Symbol.ToUpper(), tr.Quantity, tr.Price, type);
            var result =
                Validation.Validation.tradingdValidation.Invoke(cmd).ToResult();

            Publish(tr.ConnectionID, result);
            return ToResponse(Request, result);
        }

        [Route("sell"), HttpPost]
        public HttpResponseMessage PostSell([FromBody]TradingRequest tr)
            => ProcessRequest(tr, TradingType.Sell);

        [Route("buy"), HttpPost]
        public HttpResponseMessage PostBuy([FromBody]TradingRequest tr)
            => ProcessRequest(tr, TradingType.Buy);


        public IDisposable Subscribe(IObserver<CommandWrapper> observer)
            => subject.Subscribe(observer);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                subject.Dispose();
            base.Dispose(disposing);
        }
    }
}