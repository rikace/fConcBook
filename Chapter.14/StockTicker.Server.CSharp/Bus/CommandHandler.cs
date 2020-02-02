using System;
using System.Threading.Tasks;
using StockTicker.Core;
using StockTicker.Server.CSharp.Core;
using static StockTicker.Core.Models;

namespace StockTicker.Server.CSharp.Bus
{
    public static class CommandHandler
    {
        private static readonly TradingCoordinator TradingCoordinator = TradingCoordinator.Instance();
        private static readonly EventStorage.EventStorage Storage = EventStorage.EventStorage.Instance();

        public static async Task TradingCoordinatorHandle(CommandWrapper commandWrapper)
        {
            var connectionId = commandWrapper.ConnectionId;

            var command = TradingSupervisorAgent.CoordinatorMessage.NewPublishCommand(connectionId, commandWrapper);
            await TradingCoordinator.PublishCommand(command); // #E
        }

        public static void EventStorageHandle(CommandWrapper commandWrapper)
        {   
            var eventData = default(Events.Event);
            switch (commandWrapper.Command)
            {
                case TradingCommand.BuyStockCommand cmdBuy:
                    eventData = Events.Event.NewStocksBoughtEvent(commandWrapper.Id, cmdBuy.tradingRecord);
                    break;
                case TradingCommand.SellStockCommand cmdSell:
                    eventData = Events.Event.NewStocksSoldEvent(commandWrapper.Id, cmdSell.tradingRecord);
                    break;
            }

            var eventDescriptor = Events.Event.Create(commandWrapper.Id, eventData);
            Storage.SaveEvent(commandWrapper.Id, eventDescriptor);
        }
    }
}