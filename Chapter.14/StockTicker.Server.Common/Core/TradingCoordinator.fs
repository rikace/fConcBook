[<AutoOpenAttribute>]
module TradingSupervisorAgent

open System
open Microsoft.AspNet.SignalR.Hubs
open StockTicker.Core
open StockTicker.Server

// Listing 14.8 TradingSuperviser agent based to handle active trading children agent
type CoordinatorMessage =  // #N
    | Subscribe of id : string * initialAmount : float *  caller:IHubCallerConnectionContext<IStockTickerHubClient>
    | Unsubscribe of id : string
    | PublishCommand of connId : string * CommandWrapper