module SimpleAgents

open System
open System.Net

//Listing 11.1 A simple MailboxProcessor with while-loop
type Agent<'T> = MailboxProcessor<'T>

let agent =
    Agent<string>.Start(fun inbox -> async {    // #A
        while true do
            let! message = inbox.Receive()  // #B
            use client = new WebClient()
            let uri = Uri message
            let! site = client.AsyncDownloadString(uri)  // #C
            printfn "Size of %s is %d" uri.Host site.Length
    })

agent.Post "http://www.google.com"      //#D
agent.Post "http://www.microsoft.com"   //#D


//Listing 11.2 A simple MailboxProcessor with recursive function
let agent' =
    Agent<string>.Start(fun inbox ->
        let rec loop count = async {    // #A
            let! message = inbox.Receive()
            use client = new WebClient()
            let uri = Uri message
            let! site = client.AsyncDownloadString(uri)
            printfn "Size of %s is %d - total messages %d" uri.Host site.Length (count + 1)
            return! loop (count + 1) }    // #B
        loop 0)

agent'.Post "http://www.google.com"
agent'.Post "http://www.microsoft.com"

