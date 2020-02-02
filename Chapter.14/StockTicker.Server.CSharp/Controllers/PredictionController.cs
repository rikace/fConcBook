using Microsoft.AspNetCore.Mvc;
using StockTicker.Core;

namespace StockTicker.Server.CSharp.Controllers
{
    using static Models;

    [ApiController]
    [Route("api/prediction")]
    public class PredictionController : ControllerBase
    {
        [Route("predict")]
        [HttpPost]
        public OkObjectResult PostPredict([FromBody] PredictionRequest pr)
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

            return Ok(prediction);
        }
    }
}