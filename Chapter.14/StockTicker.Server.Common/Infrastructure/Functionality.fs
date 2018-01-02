namespace StockTicker.Server

open System
open System.Collections.Generic
//open Microsoft.AspNet.SignalR
//open Microsoft.AspNet.SignalR.Hubs
open StockTicker.Core

[<AutoOpenAttribute>]
module AgentModel =

    // type alias for  MailboxProcessor
    type Agent<'a> = MailboxProcessor<'a>

    // Connects error reporting to a supervising MailboxProcessor<>
    let withSupervisor (id:string) (supervisor: Agent<string * exn>) (agent: Agent<_>) =
        agent.Error.Add(fun error -> supervisor.Post (id, error)); agent

    let startAgent (agent: Agent<_>) = agent.Start()

    // simple Supervisor that handle exception thrown from
    // other agent. Not very useful here but more sophisticated
    // logic can be implemented
    let supervisor =
       Agent<string * System.Exception>.Start(fun inbox ->
         async { while true do
                    let! (agentId, err) = inbox.Receive()
                    // do something, may be re-start?
                    printfn "an error '%s' occurred in agent %s" err.Message agentId })

[<AutoOpenAttribute>]
module ThreadSafeRandom =
    open System.Threading
    open System

    let getThreadSafeRandom = new ThreadLocal<Random>(fun () -> new Random(int DateTime.Now.Ticks))

    //let private rand = new Random((int) DateTime.Now.Ticks)
    //let locker = obj()

    //let getThreadSafeRandom =
    //    fun () -> lock locker rand.NextDouble

[<AutoOpenAttribute>]
module HelperFunctions =

    // helper to get the value from dictionary
    let tryGetValues symbol (d : Dictionary<string, 'a>) =
        let success, items = d.TryGetValue(symbol)
        match success with
        | true -> Some(items)
        | false -> None

    let setOrder (orders : Treads) symbol (trading:TradingDetails) =
        let items = orders |> tryGetValues symbol
        match items with
        | Some(items) ->
            let index = items |> Seq.tryFindIndex (fun p -> p.Price = trading.Price)
            match index with
            | Some(i) ->
                orders.[symbol].[i] <- { items.[i] with Quantity = (items.[i].Quantity + trading.Quantity) }
            | None ->
                orders.[symbol].Add({ Quantity = trading.Quantity
                                      Price = trading.Price
                                      TradingType = trading.TradingType })
        | None ->
            let treads = ResizeArray<TradingDetails>([ trading ])
            orders.Add(symbol, treads)
        orders

    let createOrder symbol trading (orderType:TradingType) = {Symbol = symbol; Quantity = trading.Quantity; Price = trading.Price; OrderType = orderType}

    let updatePortfolioBySell symbol (portfolio:Portfolio) (sellOrders:Treads) price =
        let tikcerStockInPortfolio = portfolio |> tryGetValues symbol
        let ordersStockToSell = sellOrders |> tryGetValues symbol

        match ordersStockToSell, tikcerStockInPortfolio with
        | Some(orderItems), Some(portfolioItem) ->

            orderItems
            |> Seq.tryFind (fun t -> t.Price <= price)
            |> Option.map(fun orderToSell ->
                let quantityToSell =
                    if portfolioItem.Quantity >= orderToSell.Quantity then
                         orderToSell.Quantity
                    else portfolioItem.Quantity

                let revenueSell = price * (float quantityToSell)

                if portfolioItem.Quantity = quantityToSell then portfolio.Remove(symbol) |> ignore
                else
                    portfolio.[symbol] <- { Quantity = (portfolioItem.Quantity - quantityToSell)
                                            Price = price; TradingType = TradingType.Sell }

                let indexOrderSellToRemove = orderItems |> Seq.findIndex (fun t -> t.Price <= price)

                if orderItems.[indexOrderSellToRemove].Quantity = quantityToSell then
                    sellOrders.[symbol].RemoveAt(indexOrderSellToRemove)
                    if sellOrders.[symbol].Count = 0 ||
                       (sellOrders.[symbol].Count = 1 && sellOrders.[symbol].[0].Quantity = 0) then
                       sellOrders.Remove(symbol) |> ignore
                else
                    sellOrders.[symbol].[indexOrderSellToRemove] <-
                                          { orderItems.[indexOrderSellToRemove] with
                                                Quantity = (orderItems.[indexOrderSellToRemove].Quantity - quantityToSell) }
                (revenueSell, portfolio, sellOrders))
        | _ -> None

    let updatePortfolioByBuy symbol (portfolio:Portfolio) (buyOrders:Treads) cash price =
        let ordersStockToBuy = buyOrders |> tryGetValues symbol
        match ordersStockToBuy with
        | Some(orderItems) when cash >= price ->
            orderItems
            |> Seq.tryFind (fun t -> t.Price >= price)
            |> Option.map(fun trading ->
                let quantityToBuy =
                    Seq.initInfinite<int> (fun i -> i)
                    |> Seq.takeWhile
                            (fun t ->
                            t <> trading.Quantity && ((float t) * trading.Price) < cash)
                    |> Seq.length

                let tikcerStockInPortfolio = portfolio |> tryGetValues symbol

                match tikcerStockInPortfolio with
                | Some(portfolioItem) ->
                    portfolio.[symbol] <- { Quantity = portfolioItem.Quantity + quantityToBuy //(portfolio.[symbol].Quantity + quantityToBuy)
                                            Price = price; TradingType = TradingType.Buy }
                | None ->
                    portfolio.Add(symbol,  { Quantity = quantityToBuy
                                             Price = price
                                             TradingType = TradingType.Buy })
                let indexOrderToBuyToRemove = orderItems |> Seq.findIndex (fun t -> t.Price >= price)

                if orderItems.[indexOrderToBuyToRemove].Quantity = quantityToBuy then
                    buyOrders.[symbol].RemoveAt(indexOrderToBuyToRemove)
                    if buyOrders.[symbol].Count = 0 ||
                       (buyOrders.[symbol].Count = 1 && buyOrders.[symbol].[0].Quantity = 0) then
                        buyOrders.Remove(symbol) |> ignore
                else
                    buyOrders.[symbol].[indexOrderToBuyToRemove] <-
                                    { orderItems.[indexOrderToBuyToRemove] with
                                            Quantity = (orderItems.[indexOrderToBuyToRemove].Quantity - quantityToBuy) }
                (float( quantityToBuy ) * price), portfolio,buyOrders)
        | _ -> None


    let updatePortfolio cash (stock:Stock) (portfolio:Portfolio) (trades:Treads) (tradeType:TradingType) =
        match tradeType with
        | TradingType.Buy ->
            let updatedPortfolio =
                updatePortfolioByBuy stock.Symbol portfolio trades cash stock.Price
            match updatedPortfolio with
            | None -> cash, portfolio, trades
            | Some(c, p, b) -> (cash - c), p, b
        | TradingType.Sell ->
            let updatedPortfolio =
                updatePortfolioBySell stock.Symbol portfolio trades stock.Price
            match updatedPortfolio with
            | None -> cash, portfolio, trades
            | Some(r, p, s) -> (cash + r), p, s

    let getUpdatedAsset (portfolio:Portfolio) (sellOrders:Treads)  (buyOrders:Treads) cash =
        let portafolioAssets = portfolio |> Seq.map(fun item -> {OrderRecord.Symbol = item.Key; Quantity=item.Value.Quantity; Price=item.Value.Price; OrderType = item.Value.TradingType})
        let buyOrdersAsset = buyOrders |> Seq.collect(fun item ->  item.Value |> Seq.map(fun v -> {OrderRecord.Symbol = item.Key ; Price = v.Price; Quantity = v.Quantity; OrderType = TradingType.Buy}))
        let sellOrdersAsset = sellOrders |> Seq.collect(fun item ->  item.Value |> Seq.map(fun v -> {OrderRecord.Symbol = item.Key ; Price = v.Price; Quantity = v.Quantity; OrderType = TradingType.Sell}))
        {Cash=cash; Portfolio= List<OrderRecord>(portafolioAssets);BuyOrders=List<OrderRecord>(buyOrdersAsset);SellOrders= List<OrderRecord>(sellOrdersAsset)}

   // update the stock with the new price
    let changePriceStock (stock : Stock) (price : float) =
        if price = stock.Price then stock
        else
            let lastChange = price - stock.Price
            let dayOpen =
                if stock.DayOpen = 0. then price
                else stock.DayOpen
            let dayLow =
                if price < stock.DayLow || stock.DayLow = 0. then price
                else stock.DayLow
            let dayHigh =
                if price > stock.DayHigh then price
                else stock.DayHigh
            { stock with Price = price
                         LastChange = lastChange
                         DayOpen = dayOpen
                         DayLow = dayLow
                         DayHigh = dayHigh }

    // little Helper to update the Stock
    // nothing Fancy, but it works
    let private updateStock (stock : Stock) =
        let r = ThreadSafeRandom.getThreadSafeRandom.Value.NextDouble()
        if r > 0.1 then (false, stock)
        else
            let rnd = Random(int (Math.Floor(stock.Price)))
            let percenatgeChange = rnd.NextDouble() * 0.002
            let change =
                let change = Math.Round(stock.Price * percenatgeChange, 2)
                if (rnd.NextDouble() > 0.51) then change
                else -change
            let newStock = changePriceStock stock (stock.Price + change)
            (true, newStock)

    let updateStocks (stock : Stock) (stocks: Stock array) =
        let changedStock = updateStock stock
        if fst changedStock = true then
                stocks
                |> Seq.tryFindIndex (fun s -> s.Symbol = stock.Symbol)
                |> Option.map(fun idx -> stocks.[idx] <- snd changedStock
                                         snd changedStock)
        else None
