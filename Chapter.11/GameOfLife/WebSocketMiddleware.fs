namespace GameOfLife


module WebSocketMiddleware =
    open Microsoft.AspNetCore.Http
    open System
    open System.Text
    open System.Threading
    open System.Net.WebSockets
    open FSharp.Control.Tasks.V2
    
    module Middleware =
        type SocketAgentMessage =
            | Add of WebSocket
            | Dispatch of message : string
            
        let socketAgent =
            let rec sendMessage (segment : ArraySegment<byte>) (sockets : WebSocket list) acc = async {
                match sockets with
                | socket::t ->
                     if socket.State = WebSocketState.Open then
                        try 
                            do! socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None) |> Async.AwaitTask
                            return! sendMessage segment t (socket::acc)
                        with
                        | _ -> return! sendMessage segment t acc
                     else return! sendMessage segment t acc
                | [] -> return acc }
            
            MailboxProcessor<SocketAgentMessage>.Start(fun inbox ->
                let rec loop sockets = async {
                    let! msg = inbox.Receive()
                    
                    match msg with
                    | Add socket ->
                        return! loop (socket::sockets)                    
                    | Dispatch message ->
                        let buffer = Encoding.UTF8.GetBytes(message)
                        let segment = ArraySegment<byte>(buffer)
                        let! sockets' = sendMessage segment sockets []
                        return! loop sockets'
                                
                }
                loop [])
      
        let sendMessageToSockets =
            fun message -> socketAgent.Post (Dispatch message)
                
        type WebSocketMiddleware(next : RequestDelegate) =
                member __.Invoke(ctx : HttpContext) = task {
                        
                        if ctx.Request.Path = PathString("/ws") then
                            match ctx.WebSockets.IsWebSocketRequest with
                            | true ->
                                let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()  
                                socketAgent.Post (Add webSocket)

                                let buffer : byte[] = Array.zeroCreate 8192
                                
                                let! ct = Async.CancellationToken
                                let! connResult = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct)
                                printfn "WebSocket Connection %A" connResult
                                

                            | false ->
                                printfn "Problem connecting to socket"
                                ctx.Response.StatusCode <- 400
                        else
                            next.Invoke(ctx) |> ignore
                    }              
