module SyncGate

module private SantaClausProblemModule =

        open System
        open System.Threading
      
        type Agent<'T> = MailboxProcessor<'T>

        type SynGateMessage =
            | AquireLock of AsyncReplyChannel<unit>
            | Release
           
        // SynGate uses a MailboxProcessor to emulate the synchronization
        // lock of a Semaphore maintaing the asynchronous semantic of F# Async Workflow
        type SyncGate(locks, cancellationToken, ?continuation:unit -> unit) =
            let continuation = defaultArg continuation (fun () -> ())
            let agent = Agent.Start((fun inbox ->
                    let rec aquiringLock n  = async {  
                        let! msg = inbox.Receive()
                        match msg with
                        | AquireLock(reply) ->  reply.Reply()
                                         // check if the number of locks aquired 
                                         // has reached the expected number, if so,  
                                         // the internal state changes to wait the relase of a lock 
                                         // before continuing 
                                                if n < locks - 1 then return! aquiringLock (n + 1)
                                                else return! releasingLock()
                        | Release ->    return! aquiringLock (n - 1) }
                     and releasingLock() =  
                         inbox.Scan(function
                                      | Release -> Some(aquiringLock(locks - 1))
                                      | _ -> None)
                    aquiringLock 0),cancellationToken)
            interface IDisposable with
                member x.Dispose() = (agent :> IDisposable).Dispose()
            member x.AquireAsync() = agent.PostAndAsyncReply AquireLock
            member x.Release() = agent.Post Release

        // The BarrierAsync is a block synchronization mechanism, which uses a MailboxProcessor with 
        // asynchronous message passing semantic to process messages sequentially instead of using 
        // low-level concurrency lock primitives.
        // The BarrierAsync blocks the threads until it reaches the number of
        // signals excpeted, then it replies to all the workers releasing them to continue the work
        type BarrierAsync(workers, cancellationToken, ?continuation:unit -> unit) =
             let continuation = defaultArg continuation (fun () -> ())
             let agent = Agent.Start((fun inbox ->
                            let rec loop replies = async {
                                    let! (reply:AsyncReplyChannel<int>) = inbox.Receive()
                                    let replies = reply::replies
                            // check if the number of workers waiting for a reply to continue
                            // has reached the expected number, if so, the reply functions 
                            // must be invoked for all the workers waiting to be resumed  
                            // before continuing and restaring with empty reply workers
                                    if (replies.Length) = workers then
                                        replies |> List.iteri(fun index reply -> reply.Reply(index))
                                        continuation()
                                        return! loop []
                                    else return! loop replies }
                            loop []),cancellationToken)
             interface IDisposable with
                member x.Dispose() = (agent :> IDisposable).Dispose()
             member x.AsyncSignalAndWait() = agent.PostAndAsyncReply(fun reply -> reply)  


        // The cancellation token is used to stop the execution as a whole
        // when the last Year of Christams is reached
        let cancellationTokenSource = new CancellationTokenSource()
        let cancellationToken = cancellationTokenSource.Token

        // Thread-Safe implemantion of random number generator
        // using a MailboxProcessor
        let sleepFor =
            let randomGeneratorAgent = Agent.Start((fun inbox -> async {
                let random = Random(Guid.NewGuid().GetHashCode())
                while true do 
                    let! (n, reply:AsyncReplyChannel<int>) = inbox.Receive()
                    reply.Reply (random.Next(n)) }), cancellationToken)
            (fun time -> async {
                let! sleepTime = randomGeneratorAgent.PostAndAsyncReply(fun ch -> (time, ch))
                do! Async.Sleep sleepTime })

        // Thread-Safe Logging using a MailboxProcessor
        let log = 
            let logAgent = Agent.Start(fun inbox -> async {
                while true do 
                    let! msg = inbox.Receive()
                    printfn "%s" msg })
            fun msg -> logAgent.Post msg

        let elvesCount = 10
        let reindeersCount = 9
        let startingYear = ref 2006 
        let theEndOfChristams = 2015
        let elvesNeededToWakeSanta = 3

        // only a given number of elves can wake Santa
        let queueElves = new SyncGate(elvesCount, cancellationToken)
        // SyncGate is used for Santa to prevent a second group of elves 
        // from waking him if the reindeers are waiting and Santa is helping
        // the first group of elves 
        let santasAttention = new SyncGate(1, cancellationToken)
        let allReindeers = new BarrierAsync(reindeersCount, cancellationToken, (fun () -> printfn "Reindeer reunion for Christmas %d started" !startingYear))
        let threeElves = new BarrierAsync(elvesNeededToWakeSanta, cancellationToken, (fun () -> printfn "There are %d elves that are knocking Santa's door" elvesNeededToWakeSanta))
        let elvesAreInspired = new BarrierAsync(elvesNeededToWakeSanta, cancellationToken, (fun () -> printfn "Elves return to work"))

        let reindeer (id:int) = async { 
            while true do
                do! sleepFor 200
                log (sprintf "Reindeer %d is on holiday..." id) 
                do! Async.Sleep (reindeersCount * 100)
                // Wait the last Reindeer to be signaled, only all reindeers together
                // can wake Santa Claus to deliver the presents
                let! index = allReindeers.AsyncSignalAndWait() 
                log (sprintf "Reindeer %d is back from vacation... ready to work!" id) 

                // the last reindeer that arrives at the North Pole
                // wakes Santa Claus    
                if index = reindeersCount - 1 then  
                    // Santa is awake and he is preparing the sleigh,
                    // therefore he is busy and cannot help the elves 
                    do! santasAttention.AquireAsync()
                    log (sprintf "Santa has prepared the slaigh")  

                    do! sleepFor 250
                    log (sprintf "Santa delivered the toys for Christmas %d" !startingYear)   
                    do! sleepFor 200

                    if Interlocked.Increment(startingYear) = theEndOfChristams then
                        cancellationTokenSource.Cancel()

                    santasAttention.Release()
                    log (sprintf "Santa is un-harnessing the reindeer\nAll Reindeers are going back in vacation!")
                do! sleepFor 200 }        
                    
        let elf (id:int) = async {  
            do! sleepFor 2000
            while true do
                // Only 3 elves can open the door of Santa's office
                do! queueElves.AquireAsync() 
                log (sprintf "Elf %d ran out of ideas and he needs to consult santa" id)
                // Santa waits untill three elves have a problem
                let! index = threeElves.AsyncSignalAndWait()
                // the third elf awake Santa
                if index = elvesNeededToWakeSanta - 1 then
                    do! santasAttention.AquireAsync()
                    log "Ho-ho-ho ... some elves are here!\nSanta is consulting with elves..."
                // wait until all three elves have the solution
                // and inspiration for the toys                
                do! sleepFor 500
                log (sprintf "Elf %d got inspiration" id)
                let! _ = elvesAreInspired.AsyncSignalAndWait()
                if index = elvesNeededToWakeSanta - 1 then
                    santasAttention.Release()
                    log "Santa has helped the little elves!\nOK, all done - thanks!"
                // blocking other elves to disturb Santa.
                queueElves.Release()
                do! sleepFor 2000
                log (sprintf "Elf %d retires and he is going back to wwork!" id) }

    
        let santaClausProblem() =
            let reindeers = Array.init reindeersCount (fun i -> reindeer(i))
            let elves  = Array.init elvesCount (fun i -> elf(i))

            cancellationToken.Register(fun () -> 
                    (queueElves :> IDisposable).Dispose()
                    (allReindeers :> IDisposable).Dispose()
                    (threeElves :> IDisposable).Dispose()
                    (elvesAreInspired :> IDisposable).Dispose()
                    (santasAttention :> IDisposable).Dispose()
                    log "Faith has vanished from the world\nThe End of Santa Claus!!") |> ignore

            log (sprintf "Once upon in the year %d :" !startingYear)

            let santaJobs =(Seq.append elves reindeers) |> Async.Parallel |> Async.Ignore
            Async.Start(santaJobs, cancellationToken)
            cancellationTokenSource
                                            
type SantaClausProblem() =
    member x.Start() =  SantaClausProblemModule.santaClausProblem()
