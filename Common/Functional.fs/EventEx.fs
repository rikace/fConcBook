namespace FunctionalConcurrency

open System
open System.Windows
open System.Windows.Input
open System.Runtime.CompilerServices
open System.Collections.Generic

[<RequireQualifiedAccess>]
module Event =
      let takeUntil (takeTill:IEvent<'Del1,'T1>) (src:IEvent<'Del2,'T2>) =
            let customEvent = new Event<'Del2,'T2>()
            let s = src.Subscribe (fun e -> customEvent.Trigger(null,e))
            let z = takeTill.Subscribe(fun e -> s.Dispose())
            customEvent.Publish

      let selectMany (f:'T -> IEvent<'Del,'T>) (src:IEvent<'Del,'T>) =
            let customEvent = new Event<'Del,'T>()
            src
            |> Event.add (fun e -> f(e) |> Event.add (fun t -> customEvent.Trigger(null,t)))
            customEvent.Publish

      let listenOnce f evt =
        async {
          let! res = Async.AwaitEvent evt
          f res
        } |> Async.Start

      let listenUntil p evt =
        let ok = ref true
        evt |> Event.filter (fun args -> !ok && (ok := not <| p args; !ok))

      let listenWhile p evt =
        let ok = ref true
        evt |> Event.filter (fun args -> !ok && (ok := p args; !ok))

      let listenN n evt =
        let i = ref 0
        evt |> Event.filter (fun args -> incr i; !i < n)

      let skipUntil p evt =
        let ok = ref false
        evt |> Event.filter (fun args -> !ok || (ok := p args; !ok))

      let skipWhile p evt =
        let ok = ref false
        evt |> Event.filter (fun args -> !ok || (ok := not <| p args; !ok))

      let skipN n evt =
        let i = ref 0
        evt |> Event.filter (fun args -> incr i; !i >= n)

      let mapi f evt =
        let i = ref (-1)
        evt |> Event.map (fun args -> incr i; f !i args )

      let iter f evt =
        Event.add f evt

      let iteri f evt =
        let i = ref 0
        evt |> Event.add (fun args ->f !i args; incr i)

      let mergeAll evts =
        evts |> Seq.reduce Event.merge

      let reduce f evt =
        let shouldPublish = ref false
        let state = ref Unchecked.defaultof<_>
        let res = new Event<_>()
        evt |> listenOnce (fun args ->
          state := args
          evt |> Event.add (fun args ->
            let tmp = f !state args
            res.Trigger(tmp)
            state := tmp
          )
        )
        res.Publish

      let fold f state evt =
        let shouldPublish = ref false
        let state = ref state
        let res = new Event<_>()
        evt |> Event.add (fun args ->
          let tmp = f !state args
          res.Trigger(tmp)
          state := tmp
        )
        res.Publish