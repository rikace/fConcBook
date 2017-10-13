module RequestGate

type RequestGate(n:int) =
    let semaphore = new System.Threading.Semaphore(n,n)
    member x.Aquire(?timeout) = 
        async { let! ok = Async.AwaitWaitHandle(semaphore, ?millisecondsTimeout=timeout)
                if ok then return { new System.IDisposable with
                                        member x.Dispose() =
                                            semaphore.Release() |> ignore }
                else return! failwith "Handle not aquired" }


                    