using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using static StockTicker.Core.Models;

namespace StockTicker.Server.Cs.Controllers
{
    [RoutePrefix("api/prediction")]
    public class PredictionController : ApiController
    {
        [Route("predict"), HttpPost]
        public HttpResponseMessage PostPredict([FromBody]PredictionRequest pr)
        {
            var volatility =
                Simulations.Volatilities
                .TryFind(pr.Symbol).ToResult();

            var price = pr.Price;
            if (volatility.IsOk)
            {
                var calcReq =
                    new Simulations.CalcRequest(pr.NumTimesteps, pr.Price, volatility.Ok);
                price = Simulations.calcPriceCPU(calcReq);
            }

            var prediction = new PredictionResponse(price, new double[0]);
            return this.Request.CreateResponse(HttpStatusCode.OK, prediction);
        }
    }
}
