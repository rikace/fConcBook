namespace StockTicker.Core

open System
open System.Threading
open System.Collections.Generic
open StockTicker.Core

[<AutoOpenAttribute>]
module AgentModel =

    // type alias for  MailboxProcessor
    type Agent<'a> = MailboxProcessor<'a>

    // Connects error reporting to a supervising MailboxProcessor<>
    let withSupervisor (id: string) (supervisor: Agent<string * exn>) (agent: Agent<_>) =
        agent.Error.Add(fun error -> supervisor.Post(id, error))
        agent

    let startAgent (agent: Agent<_>) = agent.Start()

    // simple Supervisor that handle exception thrown from
    // other agent. Not very useful here but more sophisticated
    // logic can be implemented
    let supervisor =
        Agent<string * System.Exception>.Start
                (fun inbox ->
                async {
                    while true do
                        let! (agentId, err) = inbox.Receive()
                        // do something, may be re-start?
                        printfn "an error '%s' occurred in agent %s" err.Message agentId
                })

[<AutoOpenAttribute>]
module ThreadSafeRandom =    
    let getThreadSafeRandom = new ThreadLocal<Random>(fun () -> new Random(int DateTime.Now.Ticks))

[<AutoOpenAttribute>]
module HelperFunctions =

    // helper to get the value from dictionary
    let tryGetValues symbol (d: IDictionary<string, 'a>) =
        match d.TryGetValue(symbol) with
        | true, items -> Some(items)
        | false, _ -> None

    let createOrder symbol (clientOrder: ClientOrder) (orderType: TradingType) =
        { OrderId = Guid.NewGuid()
          Symbol = symbol
          Quantity = clientOrder.Quantity
          Price = clientOrder.Price
          TradingType = if orderType = TradingType.Buy then "buy" else "sell" }

    let setOrder (orders: Treads) symbol (order: ClientOrder) =
        let items = orders |> tryGetValues symbol
        match items with
        | Some(items) ->
            let index = items |> Seq.tryFindIndex (fun p -> p.Price = order.Price)
            match index with
            | Some(i) ->
                let trading = { items.[i] with Quantity = (items.[i].Quantity + order.Quantity) }
                orders.[symbol].[i] <- trading
                orders, trading
            | None ->
                let trading = createOrder symbol order order.TradingType
                orders.[symbol].Add(trading)
                orders, trading
        | None ->
            let trading = createOrder symbol order order.TradingType
            let treads = ResizeArray<OrderRecord>([ trading ])
            orders.Add(symbol, treads)
            orders, trading


    let updatePortfolioBySell symbol (portfolio: Portfolio) (sellOrders: Treads) price =

        let tikcerStockInPortfolio = portfolio |> tryGetValues symbol
        let ordersStockToSell = sellOrders |> tryGetValues symbol

        match ordersStockToSell, tikcerStockInPortfolio with
        | Some(orderItems), Some(portfolioItem) ->
            orderItems
            |> Seq.tryFind (fun t -> t.Price <= price)
            |> Option.map (fun orderToSell ->
                let quantityToSell =
                    if portfolioItem.Quantity >= orderToSell.Quantity then orderToSell.Quantity
                    else portfolioItem.Quantity

                let revenueSell = price * (decimal quantityToSell)
                if portfolioItem.Quantity = quantityToSell then
                    portfolio.Remove(symbol) |> ignore
                else
                    portfolio.[symbol] <- { orderToSell with
                                                Quantity = (portfolioItem.Quantity - quantityToSell)
                                                Symbol = symbol
                                                Price = price }

                let indexOrderSellToRemove = orderItems |> Seq.findIndex (fun t -> t.Price <= price)
                if orderItems.[indexOrderSellToRemove].Quantity = quantityToSell then
                    sellOrders.[symbol].RemoveAt(indexOrderSellToRemove)
                    if sellOrders.[symbol].Count = 0
                       || (sellOrders.[symbol].Count = 1 && sellOrders.[symbol].[0].Quantity = 0) then
                        sellOrders.Remove(symbol) |> ignore
                else
                    sellOrders.[symbol].[indexOrderSellToRemove] <- { orderItems.[indexOrderSellToRemove] with
                                                                          Quantity =
                                                                              (orderItems.[indexOrderSellToRemove].Quantity - quantityToSell) }
                (revenueSell, portfolio, sellOrders))
        | _ -> None

    let updatePortfolioByBuy symbol (portfolio: Portfolio) (buyOrders: Treads) cash price =
        let ordersStockToBuy = buyOrders |> tryGetValues symbol
        match ordersStockToBuy with
        | Some(orderItems) when cash >= price ->
            orderItems
            |> Seq.tryFind (fun t -> t.Price >= price)
            |> Option.map (fun trading ->
                let quantityToBuy =
                    Seq.initInfinite<int> id
                    |> Seq.takeWhile (fun t -> t <> trading.Quantity && ((decimal t) * trading.Price) < cash)
                    |> Seq.length

                let tikcerStockInPortfolio = portfolio |> tryGetValues symbol

                match tikcerStockInPortfolio with
                | Some(portfolioItem) ->
                    portfolio.[symbol] <- { trading with
                                                Quantity = portfolioItem.Quantity + quantityToBuy 
                                                Symbol = symbol
                                                Price = price }
                | None ->
                    portfolio.Add
                        (symbol,
                         { trading with
                               Quantity = quantityToBuy
                               Price = price
                               Symbol = symbol })
                        
                let indexOrderToBuyToRemove = orderItems |> Seq.findIndex (fun t -> t.Price >= price)

                if orderItems.[indexOrderToBuyToRemove].Quantity = quantityToBuy then
                    buyOrders.[symbol].RemoveAt(indexOrderToBuyToRemove)
                    if buyOrders.[symbol].Count = 0
                       || (buyOrders.[symbol].Count = 1 && buyOrders.[symbol].[0].Quantity = 0) then
                        buyOrders.Remove(symbol) |> ignore
                else
                    buyOrders.[symbol].[indexOrderToBuyToRemove] <- { orderItems.[indexOrderToBuyToRemove] with
                                                                          Quantity =
                                                                              (orderItems.[indexOrderToBuyToRemove].Quantity - quantityToBuy) }
                (decimal (quantityToBuy) * price), portfolio, buyOrders)
        | _ -> None

        
    let updatePortfolio cash (stock: Stock) (portfolio: Portfolio) (sellTrades: Treads) (buyTrades: Treads) = //(tradeType: TradingType) =        
        let updatedPortfolioAfterSell =
            updatePortfolioBySell stock.Symbol portfolio sellTrades stock.Price
        let cashUpdated, portfolioUpdated, sellTradesUpdated =     
            match updatedPortfolioAfterSell with
            | None -> cash, portfolio, sellTrades
            | Some(r, p, s) -> (cash + r), p, s
            
        
        let updatedPortfolioAfterBuy =
            updatePortfolioByBuy stock.Symbol portfolioUpdated buyTrades cashUpdated stock.Price
        let cashUpdated', portfolioUpdated', buyTradesUpdated =
            match updatedPortfolioAfterBuy with
            | None -> cashUpdated, portfolioUpdated, buyTrades
            | Some(c, p, b) -> (cashUpdated - c), p, b            
        
        cashUpdated', portfolioUpdated', sellTradesUpdated, buyTradesUpdated
                    
    let getUpdatedAsset (portfolio: Portfolio) (sellOrders: Treads) (buyOrders: Treads) cash =
        { Cash = cash
          Portfolio = portfolio
          BuyOrders = buyOrders
          SellOrders = sellOrders }


    // update the stock with the new price
    let changePriceStock (stock: Stock) (price: decimal) =
        if price = stock.Price then
            stock
        else
            let lastChange = price - stock.Price

            let dayOpen =
                if stock.DayOpen = 0M then price
                else stock.DayOpen

            let dayLow =
                if price < stock.DayLow || stock.DayLow = 0M then price
                else stock.DayLow

            let dayHigh =
                if price > stock.DayHigh then price
                else stock.DayHigh

            { stock with
                  Price = price
                  LastChange = lastChange
                  DayOpen = dayOpen
                  DayLow = dayLow
                  DayHigh = dayHigh }

    // little Helper to update the Stock
    // nothing Fancy, but it works
    let private updateStock (stock: Stock) =
        let r = ThreadSafeRandom.getThreadSafeRandom.Value.NextDouble()
        if r > 0.1 then
            (false, stock)
        else
            let rnd = Random(int (Math.Floor(stock.Price)))
            let percenatgeChange = rnd.NextDouble() * 0.075 |> decimal
            let change =
                let change = Math.Round(stock.Price * percenatgeChange, 2)
                let prob = rnd.NextDouble() > 0.51 
                if prob then change
                else -change
            
            let newStock = changePriceStock stock (stock.Price + change)
            (true, newStock)

    let updateStocks (stock: Stock) (stocks: Stock array) =
        let changedStock = updateStock stock
        if fst changedStock = true then
            stocks
            |> Seq.tryFindIndex (fun s -> s.Symbol = stock.Symbol)
            |> Option.map (fun idx ->                
                stocks.[idx] <- snd changedStock
                snd changedStock)
        else
            None
