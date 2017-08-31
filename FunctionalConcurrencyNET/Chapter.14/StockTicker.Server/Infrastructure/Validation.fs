namespace StockTicker.Validation

open System
open System
open StockTicker.Core
open System.Threading.Tasks

[<AutoOpen>]
module Validation =

     // The infix operator >>= is an alias of bind function.
    let inline (>>=) f1 f2 = Result.bind f1 f2


    let validateTicker  (input : TradingRecord) : Result<TradingRecord, string> =
        if input.Symbol = "" then Result.Error "Ticket must not be blank"
        else Result.Ok input

    let validateQuantity (input : TradingRecord) =
        if input.Quantity <= 0 || input.Quantity > 50 then Result.Error "Quantity must be positive and not be more than 50"
        else Result.Ok input

    let validatePrice (input : TradingRecord) =
        if input.Price <= 0. then Result.Error "Price must be positive"
        else Result.Ok input

    let tradingdValidation =
        validateTicker >> Result.bind validatePrice >> Result.bind validateQuantity


    let validateTickerRequestSymbol (input : TickerRecord) =
        if input.Symbol = "" then Result.Error "Ticket must not be blank"
        else Result.Ok input

    let validateTickerRequestPriceMin (input : TickerRecord) =
        if input.Price <= 0. then Result.Error "Price must be positive"
        else Result.Ok input

    let validateTickerRequestPriceMax (input : TickerRecord) =
        if input.Price >= 1000. then Result.Error "Price must be positive"
        else Result.Ok input

    let tickerRequestValidation =
        validateTickerRequestSymbol >> Result.bind validateTickerRequestPriceMin >> Result.bind validateTickerRequestPriceMax
