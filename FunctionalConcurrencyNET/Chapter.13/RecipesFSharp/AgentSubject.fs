module ActorSubjectModule

open System
open System
open System.Collections.Generic
open System.Reactive.Subjects
open System.Reactive.Linq


open System
open System.Threading

type AfterError<'state> =
| ContinueProcessing of 'state
| StopProcessing
    
type MailboxProcessor =

    static member public SpawnAgent<'a,'b>(messageHandler :'a ->'b->'b,
                                           initialState : 'b,                                         
                                           ?errorHandler: Exception -> 'a -> 'b -> AfterError<'b>)
                                        : MailboxProcessor<'a> =
        let errorHandler = defaultArg errorHandler (fun _ _ state -> ContinueProcessing(state))
        MailboxProcessor.Start(fun inbox ->
            let rec loop(state) = async {
                let! msg = inbox.Receive()
                try 
                    return! loop(messageHandler msg state)
                with
                | ex -> match errorHandler ex msg state with
                        | ContinueProcessing(newState)    -> return! loop(newState)
                        | StopProcessing        -> return ()
                }
            loop(initialState))

    static member public SpawnWorker(messageHandler,  ?errorHandler) =
        let errorHandler = defaultArg errorHandler (fun _ _ -> ContinueProcessing(()))
        MailboxProcessor.SpawnAgent((fun msg _ -> messageHandler msg; ()),
                                     (), 
                                     (fun ex msg _ -> errorHandler ex msg))



// Messages required for the mailbox loop
 type MessageSubject<'a> =
    | Add       of IObserver<'a>
    | Remove    of IObserver<'a>
    | Next      of 'a
    | Error     of exn
    | Completed

type ActorSubject<'a>() =
        let mbox = MailboxProcessor<MessageSubject<'a>>.Start(fun inbox ->
            let rec loop( observers : IObserver<'a> list) = async {
                let! req = inbox.Receive()
 
                match req with
                | MessageSubject.Add(observer) ->
                        return! loop (observer::observers)
 
                | MessageSubject.Remove(observer) ->                                            
                        return! loop (observers |> List.filter(fun f -> f <> observer))
 
                | MessageSubject.Next(value) ->                    
                        observers |> List.iter(fun o -> o.OnNext(value))
                        return! loop observers
 
                | MessageSubject.Error(err) ->                    
                        observers |> List.iter(fun o -> o.OnError(err))
                        return! loop observers
 
                | MessageSubject.Completed ->                
                        observers |> List.iter(fun o -> o.OnCompleted())
            }
            loop ([]) )
 
        /// Raises OnNext in all the observers
        member x.Next value  = MessageSubject.Next(value)  |> mbox.Post
        /// Raises OnError in all the observers
        member x.Error ex    = MessageSubject.Error(ex)    |> mbox.Post
        /// Raises OnCompleted in all the observers
        member x.Completed() = MessageSubject.Completed    |> mbox.Post
 
        interface IObserver<'a> with
            member x.OnNext value   = x.Next(value)
            member x.OnError ex     = x.Error(ex)
            member x.OnCompleted()  = x.Completed()
 
        interface IObservable<'a> with
            member x.Subscribe(observer:IObserver<'a>) =
                observer |> MessageSubject.Add |> mbox.Post
                { new IDisposable with
                    member x.Dispose() =
                        observer |> MessageSubject.Remove |> mbox.Post }
 
let b = ActorSubject<string>()
b.Add(fun s -> printfn "ciao add %s" s)
let obs1 = { new IObserver<string> with
                member x.OnNext(s) = printfn "ciao obsrever %s" s
                member x.OnCompleted() = ()
                member x.OnError(exn) = () }
                
let token = new System.Threading.CancellationTokenSource()
b.Subscribe(obs1, token.Token)
 
b.Next("hello")
b.Do(fun a -> printfn "DO %s" a).Subscribe()

 
// Web Api
// ASP.NET Web API is a framework that makes it easy to build HTTP services that reach a broad range of clients, including browsers and mobile devices. ASP.NET Web API is an ideal platform for building RESTful applications on the .NET Framework.
// HTTP is not just for serving up web pages. It is also a powerful platform for building APIs that expose services and data. HTTP is simple, flexible, and ubiquitous. Almost any platform that you can think of has an HTTP library, so HTTP services can reach a broad range of clients, including browsers, mobile devices, and traditional desktop applications.
// ASP.NET Web API is a framework for building web APIs on top of the .NET Framework.
 
 
//Functional reactive programming
//Maciej started with some great analogies to explain reactive programming. It's really difficult to find a good one. One of his examples was a simple spreadsheet. Imagine a cell with a function referencing another cell. When the value of the referenced cell changes then the original cell changes as well in reaction to the change.
//Basically reactive programming is about treating events as first class citizen. What's even cooler is that a lot of the functional programming concepts and toolkit of filter/map... can be used here as well. Sometimes people also call this functional reactive programming
 
 
// Rx is very definitely a fantastic framework for event-driven and reactive programming in general.


// Since RX is all about sequences of events/messages
// it does fit very well together with any sort of message bus or event broker.
type RxBus() =
    let subject = new Subject<obj>()
   
    member this.AsObservable<'a>() =     
        //subject.AsObservable().OfType<'a>()  
       subject.AsObservable().Where(fun t -> t :? 'a).Select(fun t -> t :?> 'a)
 
    member this.Send<'a>(item:'a) =
        subject.OnNext(item)

       
let bus = RxBus()
bus.AsObservable<int>().Do(fun t -> printfn "ciao %d" t).Subscribe() |> ignore
bus.AsObservable<string>().Do(fun t -> printfn "hello string %s" t).Subscribe() |> ignore
// The nice thing about this is that you get automatic Linq support since it is built into RX.
// So you can add message handlers that filters or transform messages.
bus.Send(8)
bus.Send("hello")

//tell how cool is object expresion to implement IDisposable in Iobestrvable
//
//The IObserver<T> and IObservable<T> interfaces provide a generalized mechanism for push-based notification, also known as the observer design pattern. The IObservable<T> interface represents the class that sends notifications (the provider); the IObserver<T> interface represents the class that receives them (the observer). T represents the class that provides the notification information. In some push-based notifications, the IObserver<T> implementation and T can represent the same type.
//
//The provider sends notifications to the observer, but before it can do that, the Observer needs subscribe, to the provider, to indicate that it wants to receive push-based notifications.
//
//So basically, we have:
//
//IObservable(Provider)
//IObserver (Observer)
//From now and on, we will be referring to those two as Provider- Observer.
//
//The observer exposes the following methods:
//
//The current data by calling IObserver<T>.OnNext
//An error condition by calling IObserver<T>.OnError
//No further data by calling IObserver<T>.OnCompleted
//The provider must implement a single method, Subscribe, that indicates that an observer wants to receive push-based notifications.
 
 
      

type RxCommand =
    | BuyTickets of string * int
    | OrderDrink of string

// Since RX is all about sequences of events/messages
// it does fit very well together with any sort of message bus or event broker.
type RxBusCommand() =
    let subject = new Subject<RxCommand>()
   
    member this.AsObservable() =    
       subject.AsObservable()

    member this.Send(item:RxCommand) =
        subject.OnNext(item)

       
let busud = RxBusCommand()

busud.AsObservable().Do(fun t -> 
                           match t with
                           | BuyTickets(name, quantity) -> printfn "I am buying %d tickets for %s" quantity name
                           | OrderDrink(drink) -> printfn "I am getting some %s to drink" drink).Subscribe() |> ignore

// The nice thing about this is that you get automatic Linq support since it is built into RX.
// So you can add message handlers that filters or transform messages.
busud.Send(BuyTickets("Opera", 2))
busud.Send(OrderDrink("Coke"))




type ChMessage =
 | Add of int
 | Get of AsyncReplyChannel<int>

let agent = MailboxProcessor<ChMessage>.Start(fun inbox ->
                    let rec loop n = async{
                        let! msg = inbox.Receive()
                        match msg with
                        | Add(i) -> return! loop (n + i)
                        | Get(r) -> async {  do! Async.Sleep  10000
                                             r.Reply(n)} |> Async.Start
                                    return! loop n
                        }
                    loop 0)

agent.Post(Add 7)
agent.Post(Add 9)
agent.Post(Add 11)
Async.StartWithContinuations(agent.PostAndAsyncReply(fun ch -> Get(ch)),
                (fun r -> printfn "result %d" r),
                (fun e -> ()),
                (fun e -> ()))
