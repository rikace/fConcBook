namespace StockTicker.Server.FSharp

module TradingCoordinator = 

    open System
    open Microsoft.AspNetCore.SignalR
    open StockTicker.Core
    open TradingAgent
    open TwitterEmotionAnalysis.CSharp.RxPublisherSubscriber
    
    // responsible for subscribing and un-subscribing TradingAgent
    // it uses a mix of RX and Agent.Post just for demo purpose
    // (TradingAgent : IOboservable) and (TradingSuperviserAgent : IObservable)
    type TradingCoordinator() =   // #A

        //Listing 6.6 Reactive Publisher Subscriber in C#
        let subject = new RxPubSub<Trading>()    // #B
        static let tradingCoordinator =
            System.Lazy<TradingCoordinator>.Create(fun () -> new TradingCoordinator())  // #C

        let coordinatorAgent =
            Agent<CoordinatorMessage>.Start(fun inbox ->
                let rec loop (agents : Map<string, (IObserver<Trading> * IDisposable)>) =
                    async {
                        let! msg = inbox.Receive()
                        match msg with
                        | Subscribe(connId, username, amount, caller) ->    // #D
                            let observer = TradingAgent(connId, username, amount, caller)  // #E                            
                            let dispObsrever = subject.Subscribe(observer)
                            observer.Agent |> withSupervisor connId supervisor |> startAgent   // #F                            
                            do! caller.SendAsync("setInitialAsset", amount) |> Async.AwaitTask    // #G
                            return! loop (Map.add connId (observer :> IObserver<Trading>, dispObsrever) agents)
                        | Unsubscribe(connId) ->
                            match Map.tryFind connId agents with
                            | Some(_, disposable) ->  // #H
                                disposable.Dispose()
                                return! loop (Map.remove connId agents)
                            | None -> return! loop agents
                        | PublishCommand(connId, command) ->   // #I
                            match Map.tryFind connId agents with
                            | Some(agent, _) ->
                                match command.Command with
                                | TradingCommand.BuyStockCommand(id, trading) ->    
                                    agent.OnNext(Trading.Buy(trading.Symbol, trading))
                                    return! loop agents
                                | TradingCommand.SellStockCommand(connId, trading) ->
                                    agent.OnNext(Trading.Sell(trading.Symbol, trading))
                                    return! loop agents
                            | None -> return! loop agents }
                loop (Map.empty))

        member this.Subscribe(connId : string, username:string, initialAmount : decimal, caller:IClientProxy) =
            coordinatorAgent.Post(Subscribe(connId, username, initialAmount, caller)) // #D

        member this.Unsubscribe(id : string) = coordinatorAgent.Post(Unsubscribe(id))

        member this.PublishCommand(command) = coordinatorAgent.Post(command)  // #L

        member this.AddPublisher(observable : IObservable<Trading>) = subject.AddPublisher(observable)  // #M

        static member Instance() = tradingCoordinator.Value  // #C

        interface IDisposable with   // #O
            member x.Dispose() =  subject.Dispose()