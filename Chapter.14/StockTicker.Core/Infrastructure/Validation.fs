namespace StockTicker.Core

open StockTicker.Core

[<AutoOpen>]
module Validation =

     // The infix operator >>= is an alias of bind function.
    let inline (>>=) f1 f2 = Result.bind f1 f2

    let validateTicker  (input : ClientOrder) : Result<ClientOrder, string> =
        if input.Symbol = "" then Result.Error "Ticket must not be blank"
        else Result.Ok input

    let validateQuantity (input : ClientOrder) =
        if input.Quantity <= 0 || input.Quantity > 50 then Result.Error "Quantity must be positive and not be more than 50"
        else Result.Ok input

    let validatePrice (input : ClientOrder) =
        if input.Price <= 0M then Result.Error "Price must be positive"
        else Result.Ok input

    let tradingdValidation =
        validateTicker >> Result.bind validatePrice >> Result.bind validateQuantity


    let validateTickerRequestSymbol (input : ClientOrder) =
        if input.Symbol = "" then Result.Error "Ticket must not be blank"
        else Result.Ok input

    let validateTickerRequestPriceMin (input : ClientOrder) =
        if input.Price <= 0M then Result.Error "Price must be positive"
        else Result.Ok input

    let validateTickerRequestPriceMax (input : ClientOrder) =
        if input.Price >= 1000M then Result.Error "Price must be positive"
        else Result.Ok input

    let tickerRequestValidation =
        validateTickerRequestSymbol >> Result.bind validateTickerRequestPriceMin >> Result.bind validateTickerRequestPriceMax
