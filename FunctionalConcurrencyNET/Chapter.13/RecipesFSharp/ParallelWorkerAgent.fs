//---------------------------------------------------------------------------
// This is a demonstration script showing a background asynchronous
// worker agent in F#. The agent runs a set of jobs in parallel, collecting and
// reporting the incremental results.
//
// This sample code is provided "as is" without warranty of any kind.
// We disclaim all warranties, either express or implied, including the
// warranties of merchantability and fitness for a particular purpose.

namespace ParallelWorkers

open System.Threading
open System.Collections.Generic
open Microsoft.FSharp.Control

/// The internal type of messages used by BackgroundParallelWorker
type internal Message<'Index,'Data> =
    | Request of 'Index * Async<'Data>
    | Result of 'Index * 'Data
    | Clear

/// A component that accepts a collection of jobs to run in the
/// background and reports progress on these jobs.
///
/// This component can be used from any thread with a synchronization
/// context, e.g. a GUI thread or an ASP.NET page handler. Events
/// reporting progress are raised on the thread implied by the
/// synchronization context, i.e. the GUI thread or the ASP.NET page
/// handler.
type BackgroundParallelWorker<'Data>(jobs:seq<Async<'Data>>) =

    let syncContext = SynchronizationContext.Current
    let raiseEventOnGuiThread (event:Event<_>) args =
        syncContext.Post(SendOrPostCallback(fun _ -> event.Trigger args),state=null)

    let changed   = new Event<_>()
    let completed = new Event<_>()

    // Check that we are being called from a GUI thread
    do match syncContext with
        | null -> failwith "Failed to capture the synchronization context of the calling thread. The System.Threading.SynchronizationContext.Current of the calling thread is null"
        | _ -> ()

    let mbox = MailboxProcessor<_>.Start(fun mbox ->

        let jobs = Seq.toArray jobs

        let rec ProcessMessages(results: Map<_,_>) =

            // Read messages...
            async { let! msg = mbox.Receive()
                    match msg with
                    | Result (i,v) ->

                        // Update the 'results' set and process more messages
                        let results = AddResult(results,i,Some(v))
                        return! ProcessMessages(results)

                    | Clear ->
                        raiseEventOnGuiThread changed Map.empty
                        return! ProcessMessages(Map.empty)

                    | Request(i,job) ->

                        // Spawn a request work item
                        do! Async.StartChild
                              (async { let! res = job
                                       do mbox.Post(Result(i,res)) }) |> Async.Ignore

                        // Update the 'results' set and process more messages
                        let results = AddResult(results,i,None)
                        return! ProcessMessages(results)  }

        and AddResult(results,i,v) =
            let results = results.Add(i,v)

            // Fire the 'results changed' event in the initial synchronization context
            raiseEventOnGuiThread changed results

            // Fire the 'completed' event in the initial synchronization context
            if results.Count = jobs.Length && results |> Map.forall (fun _ v -> v.IsSome) then
                raiseEventOnGuiThread completed (results |> Map.map (fun k v -> v.Value))

            results

        ProcessMessages(Map.empty))

    member x.Start() =
        mbox.Post(Clear)
        for i,job in Seq.mapi (fun i x -> (i,x)) jobs do
            mbox.Post(Request(i,job))

    member x.ResultSetChanged = changed.Publish
    member x.ResultsComplete = completed.Publish


/// A component that accepts an initial index and a function that
/// asynchronously generates new indexes from existing ones. Progress
/// is reported by firing events on the GUI thread.
///
/// This component can be used from any thread with a synchronization
/// context, e.g. a GUI thread or an ASP.NET page handler. Events
/// reporting progress are raised on the thread implied by the
/// synchronization context, i.e. the GUI thread or the ASP.NET page
/// handler.
type BackgroundParallelCrawl<'Index,'Data when 'Index : comparison>(visitor: 'Index -> Async<'Index list * 'Data>,limit) =
    let syncContext = System.Threading.SynchronizationContext.Current

    let sendBack f = syncContext.Send(System.Threading.SendOrPostCallback(fun _ -> f()),state=null)

    let changeEvent = new Event<_>()

    /// This is the mailbox where we receive messages
    let mbox =
        MailboxProcessor<Message<'Index,_>>.Start(fun mbox ->
            /// This is the main state of the mailbox processor
            let rec ProcessMessages(visited: Map<_,_>) =

                async { // Read messages...
                        let! msg = mbox.Receive()
                        match msg with

                        | Clear ->
                            sendBack(fun () -> changeEvent.Trigger(Map.empty))
                            return! ProcessMessages(Map.empty)
                        | Request(url,item) ->

                            // Check we've not reached our limit and not seen this URL before
                            if visited.Count < limit && not(visited.ContainsKey(url)) then

                                // Spawn a request work item
                                do! Async.StartChild
                                      (async { let! res = item
                                               mbox.Post(Result(url,res)) }) |> Async.Ignore

                                // Update the 'visited' set and process more messages
                                let visited = AddResult(visited,url,None)
                                return! ProcessMessages(visited)

                            else

                                // Continue to process messages (results may be coming in)
                                return! ProcessMessages(visited)

                        | Result (url,(links,html)) ->

                            // Post new requests for each of the links
                            for link in links do
                                mbox.Post(Request(link,visitor link))

                            // Update the 'visited' set and process more messages
                            let visited = AddResult(visited,url,Some(links))
                            return! ProcessMessages(visited) }

            and AddResult(visited,url,results) =
                let visited = visited.Add(url,results)
                // Fire the 'results changeEvent3' event in the initial synchronization context
                do sendBack(fun () -> changeEvent.Trigger(visited))
                visited

            ProcessMessages(Map.empty) )

    member x.Start(url) =
        mbox.Post(Clear)
        mbox.Post(Request(url, visitor url))

    member x.CrawlSetChanged = changeEvent.Publish

