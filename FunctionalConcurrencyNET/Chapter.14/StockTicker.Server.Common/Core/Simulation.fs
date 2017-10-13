namespace StockTicker

open System
open StockTicker

module Simulations =
    open System
    open System.Threading
    open System.Threading.Tasks
    open FSharp.Collections.ParallelSeq

    let simulationCount = 1000000
    let riskFreeRate = 0.0215 // ten year US Treasury rate - June June 14, 2017

    let Volatilities =
        ["MSFT", 0.19
         "APPL", 0.20
         "AMZN", 0.21
         "GOOG", 0.18
         "FB", 0.20]
        |> Map.ofList

    type CalcRequest =
        {
            TimeSteps : int
            Price : float // OpenPrice / SpotPrice
            Volatility : float
        }

    let nextDoubleNormal (rnd:Random) =
        let boxMuller u1 u2 =
            Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2)
        boxMuller (rnd.NextDouble()) (rnd.NextDouble())

    let calcPriceCPU (req: CalcRequest) =
        let r = riskFreeRate
        let sigma = req.Volatility
        let dt = 1.0
        let numTimesteps = req.TimeSteps
        let spotPrice = req.Price
        let tenor = float <| req.TimeSteps

        let drift = (r - (0.5 * sigma * sigma)) * dt
        let diffusion = sigma * float(Math.Sqrt(dt))

        let meanPrice =
            [1..simulationCount]
            |> PSeq.averageBy (fun _ ->
                let rnd = Random(int(DateTime.Now.Ticks))
                let finalVal =
                    [1..numTimesteps]
                    |> Seq.fold (fun s _ ->
                        s * float(Math.Exp(drift + (diffusion * (nextDoubleNormal rnd))))
                       ) 1.0
                spotPrice * finalVal
            )

        let discountFactor = float <| Math.Exp(-1.0 * riskFreeRate * tenor)
        meanPrice * discountFactor

