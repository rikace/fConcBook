module AsyncDBAgent

open System
open System.Data
open System.Data.SqlClient
open System.IO
open System.Configuration
open System.Threading
open FunctionalConcurrency

//Listing 11.3 Using MailboxProcessor to manage database calls and avoid bottlenecks
type Person  =
    { id:int; firstName:string; lastName:string; age:int }   // #A

type SqlMessage =
    | Command of id:int * AsyncReplyChannel<Person option>   // #B

let agentSql connectionString =
    fun (inbox: MailboxProcessor<SqlMessage>) ->
        let rec loop() = async {
            let! Command(id, reply) = inbox.Receive() // #C
            use conn = new SqlConnection(connectionString)
            use cmd = new SqlCommand("Select FirstName, LastName, Age from db.People where id = @id")
            cmd.Connection <- conn
            cmd.CommandType <- CommandType.Text
            cmd.Parameters.Add("@id", SqlDbType.Int).Value <- id
            if conn.State <> ConnectionState.Open then
                do! conn.OpenAsync()    // #D
            use! reader = cmd.ExecuteReaderAsync(  // #E
                            CommandBehavior.SingleResult ||| CommandBehavior.CloseConnection)
            let! canRead = (reader:SqlDataReader).ReadAsync()
            if canRead then
                let person =
                    {   id = reader.GetInt32(0)
                        firstName = reader.GetString(1)
                        lastName = reader.GetString(2)
                        age = reader.GetInt32(3)  }
                reply.Reply(Some person)    // #F
            else reply.Reply(None)    // #G
            return! loop() }
        loop()

type AgentSql(connectionString:string) =
    let agentSql = new MailboxProcessor<SqlMessage>(agentSql connectionString)

    member this.ExecuteAsync (id:int) =
        agentSql.PostAndAsyncReply(fun ch -> Command(id, ch)) // #H

    member this.ExecuteTask (id:int) =
        agentSql.PostAndAsyncReply(fun ch -> Command(id, ch)) |> Async.StartAsTask  // #H



//Listing 11.4 Interacting asynchronously with AgentSql
let ``Listing 11.4`` () =
    let token = CancellationToken()    // #A

    let agentSql = AgentSql("< Connection String Here >")
    let printPersonName id = async {
        let! (Some person) = agentSql.ExecuteAsync id    // #B
        printfn "Fullname %s %s" person.firstName person.lastName
    }

    Async.Start(printPersonName 42, token)    // #C

    Async.StartWithContinuations(agentSql.ExecuteAsync 42,  // #D
        (fun (Some person) -> printfn "Fullname %s %s" person.firstName person.lastName),    // #E
        (fun exn -> printfn "Error: %s" exn.Message),    // #E
        (fun cnl -> printfn "Operation cancelled"), token)    // #E
