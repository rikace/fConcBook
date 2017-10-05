namespace RxPipeClient

open System
open System.IO
open System.IO.Pipes
open System.Text
open System.Threading
open System.Reactive
open System.Reactive.Subjects
open System.Reactive.Linq
open FSharp.Control.Reactive
open FunctionalConcurrency
open Common

type PipeClientAsync(namePipe:string, serverName:string) as this =
    let clientPipe = new NamedPipeClientStream(serverName, namePipe, PipeDirection.InOut,
                                                            PipeOptions.Asynchronous ||| PipeOptions.WriteThrough,
                                                            Security.Principal.TokenImpersonationLevel.Impersonation)
    [<Literal>]
    let bufferSize = 0x1000

    let log msg = printfn "%s" msg
    let token = new CancellationTokenSource()

    //let startReadingStream (action:(PipeClientAsync * string) -> unit) =
    //    log (sprintf "Pipe Client is starting listening...")
    //    let rec loopReading bytes (sb:StringBuilder) = async {
    //        let! bytesRead = clientPipe.AsyncRead(bytes,0,bytes.Length)
    //        log (sprintf "Pipe Client readed %d bytes" bytesRead)
    //        if bytesRead > 0 then
    //            sb.Append(Encoding.Unicode.GetString(bytes, 0, bytesRead)) |> ignore
    //            Array.Clear(bytes, 0, bytes.Length)

    //        if bytesRead = bytes.Length then
    //            return! loopReading bytes sb
    //        else
    //            log (sprintf "Pipe Client message received and completed")
    //            action (this, sb.ToString())
    //            return! loopReading bytes (sb.Clear()) }
    //    //loopReading (Array.zeroCreate<byte> bufferSize) (new StringBuilder())
    //    Async.Start(loopReading (Array.zeroCreate<byte> bufferSize) (new StringBuilder()), token.Token)


    member this.StreamAsObservable() =
        log (sprintf "Pipe Server is starting listening...")
        if clientPipe.IsConnected then
            ObservableDataStreams.ReadObservable(clientPipe, bufferSize, token.Token)
        else Observable.Throw(new Exception("Client no connected"))

    member this.Connect() =
        let connect = async {
            if not <| clientPipe.IsConnected then
                log (sprintf "Connecting Pipe Client...")
                do! clientPipe.ConnectAsync()
                clientPipe.ReadMode <- PipeTransmissionMode.Byte
                log (sprintf "Pipe Client connected")
                 }
        Async.StartAsTask(connect, cancellationToken=token.Token)

    //member this.Connect() =
    //    Observable.FromAsync(fun () -> clientPipe.ConnectAsync())
    //    |> Observable.whileLoop(fun () -> not <| clientPipe.IsConnected)
    //    |> Observable.retry
    //    |> Observable.publish
    //    |> Observable.refCount

        //|> Observable.subscribe(fun _ ->
        //    log (sprintf "Pipe Server listening...")
        //    clientPipe.ReadMode <- PipeTransmissionMode.Byte
        //    startReadingStreamReactive readCallback )

    member this.Stop() =
        token.Cancel()
        clientPipe.Close()
        clientPipe.Dispose()

    interface IDisposable with
        member x.Dispose() = this.Stop()
