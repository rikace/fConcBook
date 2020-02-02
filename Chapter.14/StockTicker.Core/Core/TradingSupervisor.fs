namespace StockTicker.Core

type Agent<'a> = MailboxProcessor<'a>

[<AutoOpenAttribute>]
module TradingSupervisorAgent =

    open Microsoft.AspNetCore.SignalR   
    open StockTicker.Core

    // Listing 14.8 TradingSupervisor agent based to handle active trading children agent
    type CoordinatorMessage =  // #N
        | Subscribe of connId : string * userName:string * initialAmount : decimal * caller:IClientProxy
        | Unsubscribe of id : string
        | PublishCommand of connId : string * CommandWrapper
        