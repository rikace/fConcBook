[<AutoOpen>]
module ParallelWorkersAgent

open System
open System.Threading

//Listing 11.5 Parallel MailboxProcessor workers
type MailboxProcessor<'a> with
    static member public parallelWorker' (workers:int)         //#A
            (behavior:MailboxProcessor<'a> -> Async<unit>)     //#B
            (?errorHandler:exn -> unit) (?cts:CancellationToken) =

        let cts = defaultArg cts (CancellationToken())         //#C
        let errorHandler = defaultArg errorHandler ignore      //#C
        let agent = new MailboxProcessor<'a>((fun inbox ->
            let agents = Array.init workers (fun _ ->          //#D
                let child = MailboxProcessor.Start(behavior, cts)
                child.Error.Subscribe(errorHandler) |> ignore  //#E
                child)
            cts.Register(fun () ->
                agents |> Array.iter(    //#F
                    fun a -> (a :> IDisposable).Dispose()))
            |> ignore

            let rec loop i = async {
                let! msg = inbox.Receive()
                agents.[i].Post(msg)    //#G
                return! loop((i+1) % workers)
            }
            loop 0), cts)
        agent.Start()


open SimpleAgents

type AgentDisposable<'T>(f:MailboxProcessor<'T> -> Async<unit>,
                            ?cancelToken:System.Threading.CancellationTokenSource) =
    let cancelToken = defaultArg cancelToken (new CancellationTokenSource())
    let agent = MailboxProcessor.Start(f, cancelToken.Token)

    member x.Agent = agent
    interface IDisposable with
        member x.Dispose() = (agent :> IDisposable).Dispose()
                             cancelToken.Cancel()

type AgentDisposable<'T> with
    //member this.withSupervisor (supervisor: Agent<exn>) (transform) =
    //    this.Agent.Error.Add(fun error -> supervisor.Post(transform(error))); this

    member this.withSupervisor (supervisor: Agent<exn>) =
        this.Agent.Error.Add(supervisor.Post); this


type MailboxProcessor<'a> with
    static member public parallelWorker (workers:int, behavior:MailboxProcessor<'a> -> Async<unit>, ?errorHandler, ?cancelToken:CancellationTokenSource) =
        let cancelToken = defaultArg cancelToken (new System.Threading.CancellationTokenSource())
        let thisletCancelToken = cancelToken.Token
        let errorHandler = defaultArg errorHandler ignore
        let supervisor = Agent<System.Exception>.Start(fun inbox -> async {
                            while true do
                                let! error = inbox.Receive()
                                errorHandler error })
        let agent = new MailboxProcessor<'a>((fun inbox ->
            let agents = Array.init workers (fun _ ->
                (new AgentDisposable<'a>(behavior, cancelToken))
                    .withSupervisor supervisor )
            thisletCancelToken.Register(fun () ->
                agents |> Array.iter(fun agent -> (agent :> IDisposable).Dispose())
            ) |> ignore
            let rec loop i = async {
                let! msg = inbox.Receive()
                agents.[i].Agent.Post(msg)
                return! loop((i+1) % workers)
            }
            loop 0), thisletCancelToken)
        agent.Start()
        agent



open AsyncDBAgent
open System.Configuration

//Listing 11.6 Using the agent parallelWorkers to parallelize database reads

let connectionString =    // #A
    ConfigurationManager.ConnectionStrings.["DbConnection"].ConnectionString
let maxOpenConnection = 10    // #B

let agentParallelRequests =
    MailboxProcessor<SqlMessage>.parallelWorker(maxOpenConnection, // #C
                                                    agentSql connectionString)

let fetchPeopleAsync (ids:int list) =
    let asyncOperation =    // #D
        ids
        |> Seq.map (fun id ->
            agentParallelRequests.PostAndAsyncReply(fun ch -> Command(id, ch)))
        |> Async.Parallel
    Async.StartWithContinuations(asyncOperation,    // #e
            (fun people -> people |> Array.choose id
                                  |> Array.iter(fun person ->
                printfn "Fullname %s %s" person.firstName person.lastName)),
            (fun exn -> printfn "Error: %s" exn.Message),
            (fun cnl -> printfn "Operation cancelled"))