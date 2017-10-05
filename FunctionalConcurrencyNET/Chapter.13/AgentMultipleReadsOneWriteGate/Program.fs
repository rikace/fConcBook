open Agents4DB
open System
open System.Threading.Tasks
open System.Configuration
open System.Collections.Generic

let connectionString = ConfigurationManager.ConnectionStrings.["DbConnection"].ConnectionString
let maxOpenConnection = 10

let myDB = Dictionary<int, Person>()

let agentSql connectionString =
    fun (inbox: MailboxProcessor<_>) ->
        let rec loop() = async {
            let! msg = inbox.Receive()

            match msg with
            | Read(Get(id, reply)) ->
                match myDB.TryGetValue(id) with
                | true, res -> reply.Reply(Some res)
                | _ -> reply.Reply(None)
            | Write(Add(person, reply)) ->
                let id = myDB.Count;
                myDB.Add(id, {person with id = id})
                reply.Reply(Some id)

            return! loop() }
        loop()


let agent =
    ReaderWriterAgent(maxOpenConnection, agentSql connectionString)

let generator pref N =
    async {
        for i in [1..N] do
            let person = { id = -1; firstName = i.ToString(); lastName = pref; age = i }
            let! id = agent.Write(fun ch -> Add(person, ch))
            printfn "Add Person(%s %s) id=%A" person.firstName person.lastName id
            do! Async.Sleep(100)
    }

let accessor N =
    let rnd = Random((int)DateTime.Now.Ticks)
    async {
        for _ in [1..N] do
            let total = myDB.Count * 3 / 2;
            if total > 0  then
                let ind = rnd.Next(total)
                let! resp = agent.Read(fun ch -> Get(ind, ch))
                match resp with
                | Some (p) ->   printfn "%d => Person(%s %s)" ind p.firstName p.lastName
                | None ->       printfn "%d => Not Found" ind
            do! Async.Sleep(110)
    }

let start () =
    [
        generator "A" 20
        accessor 30
        generator "B" 20
        accessor 30
        accessor 30
        accessor 30
        accessor 30
    ]
    |> Async.Parallel
    |> Async.RunSynchronously


[<EntryPoint>]
let main argv =
    start() |> ignore
    0 // return an integer exit code
