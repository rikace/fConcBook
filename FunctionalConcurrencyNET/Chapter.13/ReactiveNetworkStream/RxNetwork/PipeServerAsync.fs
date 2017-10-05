namespace RxPipeServer

open System
open System.IO.Pipes
open System.Text
open System.Threading
open System.Reactive
open System.Reactive.Subjects
open System.Reactive.Linq
open FSharp.Control.Reactive
open FunctionalConcurrency
open Common

type PipeServerAsync(pipeName) =

    [<Literal>]
    let bufferSize = 0x1000

    // -1 maxNumberOfServerInstances is set to -1 (number of server instances with the same pipe name is limited only by system resources) in NamedPipeServerStream constructor
    let serverPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, -1, PipeTransmissionMode.Byte,
                         PipeOptions.WriteThrough ||| PipeOptions.Asynchronous, bufferSize, bufferSize)
    do serverPipe.ReadMode <- PipeTransmissionMode.Byte

    let log msg = printfn "%s" msg
    let token = new CancellationTokenSource()


    member this.Write (message:byte[]) =
        log (sprintf "Pipe Server sending message")
        if serverPipe.IsConnected && serverPipe.CanWrite then
            let write = async {
                do! serverPipe.AsyncWrite(message,0, message.Length)
                do! serverPipe.FlushAsync()
                serverPipe.WaitForPipeDrain() }
            Async.Start(write, token.Token)

    member this.WriteText (text:string) =
        Encoding.Unicode.GetBytes(text) |> this.Write

    member this.Connect() =
        let handleServer = async {
            if not <| serverPipe.IsConnected then
                do! serverPipe.WaitForConnectionAsync()
                log (sprintf "Pipe Server listening...") }
        Async.StartAsTask(handleServer, cancellationToken=token.Token)

    member this.Stop() =
        token.Cancel()
        serverPipe.Close()
        serverPipe.Dispose()

    interface IDisposable with
        member this.Dispose() = this.Stop()
