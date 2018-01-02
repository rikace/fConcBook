using StockTicker.Server.Cs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static StockTicker.Core.Models;
using StockTicker.Events;
using static StockTicker.Events.Events;

namespace StockTicker.Server.Cs.Bus
{
    public static class CommandHandler
    {
        private static TradingCoordinator tradingCoordinator = TradingCoordinator.Instance();
        private static EventStorage.EventStorage eventStorage = new EventStorage.EventStorage();

        public static void Handle (CommandWrapper commandWrapper)
        {
            var connectionId = commandWrapper.ConnectionId;

            var command = TradingSupervisorAgent.CoordinatorMessage.NewPublishCommand(connectionId, commandWrapper);
            tradingCoordinator.PublishCommand(command);   // #E

            Event eventData;
            if (commandWrapper.Command is TradingCommand.BuyStockCommand cmdBuy)
            {
                eventData = Event.NewStocksBuyedEvent(commandWrapper.Id, cmdBuy.tradingRecord);
            }
            else if (commandWrapper.Command is TradingCommand.SellStockCommand cmdSell)
            {
                eventData = Event.NewStocksSoldEvent(commandWrapper.Id, cmdSell.tradingRecord);
            }
            else
            {
                throw new Exception("Unsupported command type");
            }

            var eventDescriptor = Event.Create(commandWrapper.Id, eventData);
            eventStorage.SaveEvent(Guid.Parse(connectionId), eventDescriptor);
        }
    }
}