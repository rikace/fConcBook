[<AutoOpenAttribute>]
module TradingSupervisorAgent

open System
open Microsoft.AspNet.SignalR.Hubs
open StockTicker.Core
open StockTicker.Server
open TradingAgent
open RxPublisherSubscriber

// Listing 14.8 TradingSuperviser agent based to handle active trading children agent
type CoordinatorMessage =  // #N
    | Subscribe of id : string * initialAmount : float *  caller:IHubCallerConnectionContext<IStockTickerHubClient>
    | Unsubscribe of id : string
    | PublishCommand of connId : string * CommandWrapper

// responsible for subscribing and un-subscribing TradingAgent
// it uses a mix of RX and Agent.Post just for demo purpose
// (TradingAgent : IOboservable) and (TradingSuperviserAgent : IObservable)
type TradingCoordinator() =   // #A

    //Listing 6.6 Reactive Publisher Subscriber in C#
    let subject = new RxPubSub<Trading>()    // #B
    static let tradingCoordinator =
        System.Lazy.Create(fun () -> new TradingCoordinator())  // #C

    let coordinatorAgent =
        Agent<CoordinatorMessage>.Start(fun inbox ->
            let rec loop (agents : Map<string, (IObserver<Trading> * IDisposable)>) =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Subscribe(id, amount, caller) ->    // #D
                        let observer = TradingAgent(id, amount, caller)  // #E
                        let dispObsrever = subject.Subscribe(observer)
                        observer.Agent |> withSupervisor id supervisor |> startAgent   // #F
                        caller.Client(id).SetInitialAsset(amount)   // #G
                        return! loop (Map.add id (observer :> IObserver<Trading>, dispObsrever) agents)
                    | Unsubscribe(id) ->
                        match Map.tryFind id agents with
                        | Some(_, disposable) ->  // #H
                            disposable.Dispose()
                            return! loop (Map.remove id agents)
                        | None -> return! loop agents
                    | PublishCommand(id, command) ->   // #I
                        match command.Command with
                        | TradingCommand.BuyStockCommand(id, trading) ->
                            match Map.tryFind id agents with
                            | Some(a, _) ->
                                let tradingInfo = { Quantity=trading.Quantity; Price=trading.Price; TradingType = TradingType.Buy }
                                a.OnNext(Trading.Buy(trading.Symbol, tradingInfo))
                                return! loop agents
                            | None -> return! loop agents
                        | TradingCommand.SellStockCommand(id, trading) ->
                            match Map.tryFind id agents with
                            | Some(a, _) ->
                                let tradingDetails = { Quantity=trading.Quantity; Price=trading.Price; TradingType = TradingType.Sell }
                                a.OnNext(Trading.Sell(trading.Symbol, tradingDetails))
                                return! loop agents
                            | None -> return! loop agents }
            loop (Map.empty))

    member this.Subscribe(id : string, initialAmount : float, caller:IHubCallerConnectionContext<IStockTickerHubClient>) =
        coordinatorAgent.Post(Subscribe(id, initialAmount, caller)) // #D

    member this.Unsubscribe(id : string) = coordinatorAgent.Post(Unsubscribe(id))

    member this.PublishCommand(command) = coordinatorAgent.Post(command)  // #L

    member this.AddPublisher(observable : IObservable<Trading>) = subject.AddPublisher(observable)  // #M

    static member Instance() = tradingCoordinator.Value  // #C

    interface IDisposable with   // #O
        member x.Dispose() =  subject.Dispose()