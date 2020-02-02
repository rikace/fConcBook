using System;
using System.Reactive.Subjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StockTicker.Core;
using static StockTicker.Core.Models;

namespace StockTicker.Server.CSharp.Controllers
{
    public class TradingController : Controller, IObservable<CommandWrapper>
    {
        private readonly IHubContext<Core.StockTicker> _hubContext;
        private readonly Subject<CommandWrapper> _subject;

        public TradingController(IHubContext<Core.StockTicker> hubContext)
        {
            _hubContext = hubContext;
            _subject = new Subject<CommandWrapper>();
        }


        public IDisposable Subscribe(IObserver<CommandWrapper> observer)
        {
            return _subject.Subscribe(observer);
        }

        private IActionResult Publish(string connectionId, Result<ClientOrder> cmd)
        {
            var hasObs = _subject.HasObservers;
            if (cmd.IsOk && hasObs)
            {
                var wrapped = CommandWrapper.CreateTrading(connectionId, cmd.Ok);
                _subject.OnNext(wrapped);
                return StatusCode(200);
            }

            _subject.OnError(cmd.Error);
            return StatusCode(500);
        }

        private IActionResult ProcessRequest(string connId, ClientOrder order)
        {
            var result = Validation.tradingdValidation.Invoke(order).ToResult();
            return Publish(connId, result);
        }

        public IActionResult Index()
        {
            return View("Index");
        }


        [HttpPost]
        [Route("trading/logportfolio")]
        public ActionResult<User> LogPortfolio([FromForm] User userMarket)
        {
            var userName = userMarket.Username;
            var initialCash = userMarket.Initialcash;
            return RedirectToAction(nameof(OpenDashboard), new {username = userName, initialcash = initialCash});
        }

        [HttpGet]
        [Route("trading/opendashboard")]
        public ActionResult<User> OpenDashboard(string userName, decimal initialCash)
        {
            var userModel = new User
                {Username = userName, Initialcash = initialCash, UserId = Guid.NewGuid().ToString("N")};

            // Load stocks by the username 
            return View("Market", userModel);
        }


        [Route("trading/placeorder")]
        [HttpPost]
        public IActionResult PlaceOrder([FromBody] Order data)
        {
            if (data == null)
                return BadRequest("POST body is null");

            var tradingType =
                string.Compare("buy", data.TradingType, StringComparison.OrdinalIgnoreCase) == 0
                    ? TradingType.Buy
                    : TradingType.Sell;

            var clientOrder = new ClientOrder
            {
                Price = decimal.Parse(data.Price),
                Quantity = data.Quantity,
                Symbol = data.Symbol.ToUpper(),
                TradingType = tradingType,
                UserId = data.UserId
            };

            return ProcessRequest(data.ConnId, clientOrder);
        }

        protected void Dispose()
        {
            _subject.Dispose();
        }
    }
}