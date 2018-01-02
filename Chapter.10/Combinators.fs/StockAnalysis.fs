module StockAnalysis

open System
open System.Net
open System.IO
open FunctionalConcurrency
open StockAnalyzer
open FunctionalConcurrency.AsyncOperators
open FunctionalConcurrency

let rnd = Random((int)DateTime.Now.Ticks)

// ---------------- Market analysis ------------------------------

let getStockHistory symbol (days:int) =
    asyncResult {
        let! (_, data) = processStockHistory symbol
                         |> AsyncResult.handler
        let startDate = DateTime.Now - TimeSpan.FromDays((float)days)
        return
            data
            |> Seq.filter (fun x -> x.date > startDate)
            |> Seq.sortBy (fun x -> x.date)
            |> Seq.map (fun x -> x.close)
            |> Seq.toArray
    }

let analyzeHistoricalTrend symbol =
    asyncResult {
        let! data = getStockHistory symbol (365/2)
        let trend = data.[data.Length-1] - data.[0]
        return trend
    }

// --------------- Transaction estimation ------------------------

let mutable bankAccount = 500.0 + float(rnd.Next(1000))

let getAmountOfMoney() =
    async {
        // connect to bank account
        return bankAccount
    }

let withdraw amount =
    async {
      // connect to bank account
      return
        if amount > bankAccount
        then Error(InvalidOperationException("Not enough money"))
        else
            bankAccount <- bankAccount - amount
            Ok(true)
    }


let getCurrentPrice symbol =
    async {
        let! (_,data) = processStockHistory symbol
        printfn "%s=%A" symbol (data.[0].open')
        return data.[0].open'
    }

let getStockIndex index =
    async {
        let url = sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=snl1" index
        let req = WebRequest.Create(url)
        let! resp = req.AsyncGetResponse()
        use reader = new StreamReader(resp.GetResponseStream())
        return! reader.ReadToEndAsync()
    }
    |> AsyncEx.map (fun (row:string) ->
        let items = row.Split(',')
        Double.Parse(items.[items.Length-1]))
    |> AsyncResult.handler

let calcTransactionAmount amount (price:float) =
    let readyToInvest = amount * 0.75
    let cnt = Math.Floor(readyToInvest / price)
    if (cnt < 1e-5) && (price < amount)
    then 1 else int(cnt)

// ---- Running heterogeneous asynchronous operations using Applicative Functors

let howMuchToBuy stockId : AsyncResult<_> =
    AsyncEx.lift2 (calcTransactionAmount)
          (getAmountOfMoney())
          (getCurrentPrice stockId)
    |> AsyncResult.handler

let run analyze stockId =
    analyze stockId
    |> Async.RunSynchronously
    |> function
        | Ok (total) -> printfn "I recommend to buy %d unit" total
        | Error (e) -> printfn "I do not recommend to buy now"

run howMuchToBuy "MSFT"
// ----- Composing asynchronous logical operations with  custom async combinators

let doInvest stockId =
    let shouldIBuy =
        (   (getStockIndex "^IXIC" |> gt 6200.0)
            <|||>
            (getStockIndex "^NYA" |> gt 11700.0 ))
        <&&&> ((analyzeHistoricalTrend stockId) |> gt 10.0)
        |> AsyncResult.defaultValue false

    let buy amount =
        async {
            let! price = getCurrentPrice stockId
            let! result = withdraw (price*float(amount))
            return result |> Result.bimap (fun x -> if x then amount else 0) (fun _ -> 0)
        }
    AsyncEx.ifAsync shouldIBuy
        (buy <!> (howMuchToBuy stockId))
        (AsyncEx.retn <| Error(Exception("Do not do it now")))
    |> AsyncResult.handler


