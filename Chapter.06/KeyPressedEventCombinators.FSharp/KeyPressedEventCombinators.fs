namespace KeyPressedEventCombinators

open System
open System.Reactive.Linq
open System.Reactive.Concurrency

type KeyPressedEventCombinators(secretWord, interval, keyPress:IEvent<char>) =
    let evt =
        let timer = new System.Timers.Timer(float interval) //#A
        let timeElapsed = timer.Elapsed |> Event.map(fun _ -> 'X') //#B
        let keyPressed = keyPress
                         |> Event.filter(fun kd -> Char.IsLetter kd)
                         |> Event.map(fun kd -> Char.ToLower kd) //#C
        timer.Start()  //#A
                
        keyPressed
        |> Event.merge timeElapsed //#D
        |> Event.scan(fun acc c ->
            if c = 'X' then "Game Over"
            else
                let word = sprintf "%s%c" acc c
                if word = secretWord then "You Won!"
                else word
            ) String.Empty //#E

    [<CLIEvent>]
    member this.OnKeyDown = evt //#F

type KeyPressedObservableCombinators(secretWord, interval, keyPress:IObservable<char>) =
    let evt = Event<string>()

    let timer = new System.Timers.Timer(float interval)
    do timer.Start()
    let timeElapsed = timer.Elapsed |> Observable.map(fun _ ->
        printfn "Time elapsed"
        'X')
    let keyPressed = keyPress
                        |> Observable.filter(fun c ->
                            printfn "filter"
                            Char.IsLetter c)
                        |> Observable.map(fun kd ->
                            printfn "map"
                            Char.ToLower kd)
    let disposable =
        printfn "staring"
        keyPressed
        |> Observable.merge timeElapsed
        |> Observable.scan(fun acc c ->
            if c = 'X' then "Game Over"
            else
                let word = sprintf "%s%c" acc c
                if word = secretWord then "You Won!"
                else word
            ) String.Empty
        |> Observable.subscribe(fun text -> evt.Trigger text)

    [<CLIEvent>]
    member this.OnKeyDown = evt.Publish

    interface IDisposable with
        member this.Dispose() =
            disposable.Dispose()


type ConsoleKeyPressEvent () =
    
    let isEnter key =
        Convert.ToChar(ConsoleKey.Enter) = key
        
    let subscribeOn o =
        Observable.SubscribeOn(o, Scheduler.Default)
    let obs =
        seq {
            let mutable keyPressed = Console.ReadKey().KeyChar
            while isEnter keyPressed |> not do
                yield keyPressed
                keyPressed <- Console.ReadKey().KeyChar
        } |> Observable.ToObservable |> subscribeOn
    
    let event = Event<char>()

    do
        obs.Subscribe(event.Trigger) |> ignore
        
    member this.KeysPressObservable = obs 
        
    member this.KeysPressEvent = event.Publish
