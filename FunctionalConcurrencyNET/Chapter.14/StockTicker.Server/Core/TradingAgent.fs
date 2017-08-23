[<AutoOpenAttribute>]
module TradingAgent

open System
open Microsoft.AspNet.SignalR.Hubs
open StockTicker.Core
open StockTicker.Server

// specialized agent for each user
// keep track orders and status of portfolio
type TradingAgent(connId : string, initialAmount : float, caller:IHubCallerConnectionContext<IStockTickerHubClient>) =  // #A

    let agent =
        new Agent<Trading>(fun inbox ->
            // single thread safe no sharing
            let rec loop cash (portfolio : Portfolio) (buyOrders : Treads) (sellOrders : Treads) =
                async {  // #B
                    let! msg = inbox.Receive()
                    match msg with
                    | Kill(reply) -> reply.Reply()   // #C
                    | Error(exn) -> raise exn    // #C

                    | Trading.Buy(symbol, trading) ->    // #D
                        let items = setOrder buyOrders symbol trading 
                        let order = createOrder symbol trading TradingType.Buy
                        caller.Client(connId).UpdateOrderBuy(order)
                        return! loop cash portfolio items sellOrders

                    | Trading.Sell(symbol, trading) ->    // #D
                        let items = setOrder sellOrders symbol trading
                        let order = createOrder symbol trading TradingType.Sell
                        caller.Client(connId).UpdateOrderSell(order)
                        return! loop cash portfolio buyOrders items

                    | Trading.UpdateStock(stock) ->     // #E
                        caller.Client(connId).UpdateStockPrice stock

                        let cash, portfolio, sellOrders = updatePortfolio cash stock portfolio sellOrders TradingType.Sell
                        let cash, portfolio, buyOrders = updatePortfolio cash stock portfolio buyOrders TradingType.Buy
                          
                        let asset = getUpdatedAsset portfolio sellOrders buyOrders cash   // #F
                        caller.Client(connId).UpdateAsset(asset)    // #G

                        return! loop cash portfolio buyOrders sellOrders
            }
            loop initialAmount (Portfolio(HashIdentity.Structural)) (Treads(HashIdentity.Structural))
                (Treads(HashIdentity.Structural)))

    member x.Agent = agent

    interface IObserver<Trading> with   // #H
        member x.OnNext(msg) = agent.Post(msg:Trading)      // #I
        member x.OnError(exn) = agent.Post(Error exn)       // #C
        member x.OnCompleted() = agent.PostAndReply(Kill)   // #C