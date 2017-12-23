module Program

open System
open System.Threading.Tasks
open System.Configuration
open System.Collections.Generic
open Agents4DB

let connectionString = "" //ConfigurationManager.ConnectionStrings.["DbConnection"].ConnectionString
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

let write person = async {
    let! id = agent.Write(fun ch -> Add(person, ch))
    printfn "Add person %d" person.id
    do! Async.Sleep(100)
}

let read personId = async {
    let! resp = agent.Read(fun ch -> Get(personId, ch))
    printfn "Get person %d" personId
    do! Async.Sleep(100)
}


let people =
    [1..100] |> Seq.map (fun x->
        { id = x; firstName = x.ToString(); lastName = x.ToString(); age = x })

let demo() =
    [ for person in people do
        yield write person
        yield read person.id
        yield write  person
        yield read person.id
        yield read person.id ]
        |> Async.Parallel

[<EntryPoint>]
let main argv =
    demo() |> Async.RunSynchronously
    0
