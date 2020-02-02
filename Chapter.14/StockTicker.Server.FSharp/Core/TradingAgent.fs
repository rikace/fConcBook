namespace StockTicker.Server.FSharp

open StockTicker.Core.Models

module TradingAgent =

    open System
    open StockTicker.Core
    open Microsoft.AspNetCore.SignalR
        
    // Listing 14.9 Trading Agent that represents an active user
    // specialized agent for each user
    // keep track orders and status of portfolio     
    type TradingAgent(connId : string, userName: string, initialAmount : decimal, caller:IClientProxy) =  // #A

        let agent =
            new Agent<Trading>(fun inbox ->
                // single thread safe no sharing
                let rec loop (asset:Asset) =
                    async {     // #B
                        let! msg = inbox.Receive()
                        match msg with
                        | Kill(reply) -> reply.Reply()   // #C
                        | Error(exn) -> raise exn        // #C

                        | Trading.Buy(symbol, trading) ->    // #D
                            let treads, order = HelperFunctions.setOrder asset.BuyOrders symbol trading
                            do! caller.SendAsync("updateOrderBuy", order) |> Async.AwaitTask
                            return! loop { asset with BuyOrders = treads }
                        | Trading.Sell(symbol, trading) ->    // #D
                            let treads, order = HelperFunctions.setOrder asset.SellOrders symbol trading                            
                            do! caller.SendAsync("updateOrderSell", order) |> Async.AwaitTask
                            return! loop { asset with SellOrders = treads }
                        | Trading.UpdateStock(stock) ->     // #E
                            do! caller.SendAsync("updateStockPrice", stock) |> Async.AwaitTask
                            let mutable isPortfolioUpdated = false
                            
                            let cashUpdated, portfolioUpdated, sellTreadsUpdated, buyTreadsUpdated =
                                HelperFunctions.updatePortfolio asset.Cash stock asset.Portfolio asset.SellOrders asset.BuyOrders

                            if (asset.Cash <> cashUpdated || asset.Portfolio <> portfolioUpdated) then 
                                isPortfolioUpdated <- true
                                
                            if isPortfolioUpdated then 
                                let asset = getUpdatedAsset asset.Portfolio asset.SellOrders asset.BuyOrders asset.Cash   // #F
                                do! caller.SendAsync("updateAsset", asset) |> Async.AwaitTask    // #G
                                return! loop asset 
                            

                            return! loop { asset with Cash = cashUpdated; Portfolio = portfolioUpdated; SellOrders = sellTreadsUpdated; BuyOrders = buyTreadsUpdated }
                }
                loop { Asset.Default with Cash = initialAmount })

        member this.Agent = agent

        interface IObserver<Trading> with   // #H
            member this.OnNext(msg) = agent.Post(msg:Trading)      // #I
            member this.OnError(exn) = agent.Post(Error exn)       // #C
            member this.OnCompleted() = agent.PostAndReply(Kill)   // #C