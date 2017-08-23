namespace KeyPressedEventCombinators

open System
open System.Drawing
open System.Windows.Forms

// Listing 6.1 F# Event Combinator to manage Key-Down Events
//type KeyPressedEventCombinators(secretWord, interval, control:#System.Windows.Forms.Control) =
//    let evt =
//        let timer = new System.Timers.Timer(float interval) //#A
//        let timeElapsed = timer.Elapsed |> Event.map(fun _ -> 'X') //#B
//        let keyPressed = control.KeyPress
//                         |> Event.filter(fun kd -> Char.IsLetter kd.KeyChar)
//                         |> Event.map(fun kd -> Char.ToLower kd.KeyChar) //#C
//        timer.Start()  //#A

//        keyPressed
//        |> Event.merge timeElapsed //#D
//        |> Event.scan(fun acc c ->
//            if c = 'X' then "Game Over"
//            else
//                let word = sprintf "%s%c" acc c
//                if word = secretWord then "You Won!"
//                else word
//            ) String.Empty //#E

//    [<CLIEvent>]
//    member this.OnKeyDown = evt //#F

// TODO
type KeyPressedObservableCombinators(secretWord, interval, control:#System.Windows.Forms.Control) =
    let evt = Event<string>()

    let timer = new System.Timers.Timer(float interval)
    do timer.Start()
    let timeElapsed = timer.Elapsed |> Observable.map(fun _ -> 'X')
    let keyPressed = control.KeyPress
                        |> Observable.filter(fun c -> Char.IsLetter c.KeyChar)
                        |> Observable.map(fun kd -> Char.ToLower kd.KeyChar)
    let disposable =
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


module Main =
    let font    = new Font("Microsoft Sans Serif", 10.0F,
                    FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)))
    let textBox = new TextBox(Font=font, Location=Point(6,6), Size=Size(288,28))
    let label   = new Label(Text="<State from .Scan() will be here >",
                    Font=font, Location=Point(6,40), Size=Size(288,21))
    let form    = new Form(Text = "KeyPressedEventCombinators", ClientSize = Size(300,70))
    form.Controls.Add(textBox)
    form.Controls.Add(label)

    (new KeyPressedObservableCombinators("reactive", 5000, textBox))
        .OnKeyDown.Add(fun value ->
            let assign() = label.Text <- value
            label.BeginInvoke(Action(assign)) |> ignore)

    Application.Run(form)