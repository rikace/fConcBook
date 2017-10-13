module ReplaySubject

// The ReplaySubject<T> type implements both IObserver<T> and IObservable<T>. 
// It is functionally equivalent to the class of the same name in the 
// Reactive Extensions (Rx) library with a replay buffer of a specified size .


open System
open System.Collections.Generic

type CircularBuffer<'T> (bufferSize:int) =
    let buffer = Array.zeroCreate<'T> bufferSize
    let mutable index = 0
    let mutable total = 0
    member this.Add value =
        if bufferSize > 0 then
            buffer.[index] <- value
            index <- (index + 1) % bufferSize
            total <- min (total + 1) bufferSize
    member this.Iter f =     
        let start = if total = bufferSize then index else 0
        for i = 0 to total - 1 do 
            buffer.[(start + i) % bufferSize] |> f                 

type message<'T> =
    | Add of IObserver<'T>
    | Remove of IObserver<'T>
    | Next of 'T
    | Completed
    | Error of exn

let startAgent (bufferSize:int) =
    let subscribers = LinkedList<_>()
    let buffer = CircularBuffer bufferSize               
    MailboxProcessor.Start(fun inbox ->
        let rec loop () = async {
            let! message = inbox.Receive()
            match message with
            | Add observer ->                    
                subscribers.AddLast observer |> ignore
                buffer.Iter observer.OnNext
                return! loop ()
            | Remove observer ->
                subscribers.Remove observer |> ignore
                return! loop ()
            | Next value ->                                       
                for subscriber in subscribers do
                    subscriber.OnNext value
                buffer.Add value
                return! loop () 
            | Error e ->
                for subscriber in subscribers do
                    subscriber.OnError e
            | Completed ->
                for subscriber in subscribers do
                    subscriber.OnCompleted ()
        }
        loop ()
    )

type ReplaySubject<'T> (bufferSize:int) =
    let bufferSize = max 0 bufferSize
    let agent = startAgent bufferSize    
    let subscribe observer =
        observer |> Add |> agent.Post
        { new System.IDisposable with
            member this.Dispose () =
                observer |> Remove |> agent.Post
        }
    member this.Next value = Next value |> agent.Post
    member this.Error error = Error error |> agent.Post
    member this.Completed () = Completed |> agent.Post    
    interface System.IObserver<'T> with
        member this.OnNext value = Next value |> agent.Post
        member this.OnError error = Error error |> agent.Post
        member this.OnCompleted () = Completed |> agent.Post
    member this.Subscribe(observer:System.IObserver<'T>) =
        subscribe observer
    interface System.IObservable<'T> with
        member this.Subscribe observer = subscribe observer                   
and Subject<'T>() = inherit ReplaySubject<'T>(0)

do  let subject = ReplaySubject(3)            
    use d = subject.Subscribe(fun (x:int) -> System.Console.WriteLine x)
    subject.Next(10)
    subject.Next(11)
    use d = subject.Subscribe(fun (x:int) -> System.Console.WriteLine x)
    System.Console.ReadLine() |> ignore

