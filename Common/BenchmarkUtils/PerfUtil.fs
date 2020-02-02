namespace BenchmarkUtils

open System
open System.Threading
open System.Diagnostics

module Benchmark =
    
    [<Sealed>]
    type Bench(startFresh: bool, title: string) =
        let prepareForOperation() =
            // Pre-empt a lot of other apps for more accurate results
            Process.GetCurrentProcess().PriorityClass <- ProcessPriorityClass.High
            Thread.CurrentThread.Priority <- ThreadPriority.Highest
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()
            // Force the BenchPerformance.Time to be jitted/loaded/whatever. This ensures
            // that the first use does not influence the timing.
            Bench.Time(String.Empty, true, fun () -> Thread.Sleep(0))
        do
            if startFresh then prepareForOperation()
        
        let m_gen0Start = GC.CollectionCount(0)
        let m_get1Start = GC.CollectionCount(1)
        let m_gen2Start = GC.CollectionCount(2)
        let m_startTime = Stopwatch.GetTimestamp()
        
        static member Time(text: string, operation: Action) = Bench.Time(text, true, operation);
        
        static member Time(text: string, startFresh: bool, operation: Action) =
            use o = new Bench(startFresh, text)
            let watch = new Stopwatch()
            watch.Start()
            operation.Invoke()
            watch.Stop()
            
        interface IDisposable with
            member __.Dispose() =
                let elapsedTime = Stopwatch.GetTimestamp() - m_startTime
                let milliseconds = (elapsedTime * 1000L) / Stopwatch.Frequency

                if String.IsNullOrEmpty(title) |> not then 
                    let defColor = Console.ForegroundColor
                    Console.ForegroundColor <- ConsoleColor.Yellow
                    let title = sprintf "\tOperation > %s <" title
                    let gcInfo =
                        sprintf "\tGC(G0=%d, G1=%d, G2=%d)\n\tTotal Time  %dms\n"
                            (GC.CollectionCount(0) - m_gen0Start) (GC.CollectionCount(1) - m_get1Start) (GC.CollectionCount(2) - m_gen2Start) milliseconds 

                    Console.WriteLine(String('*', gcInfo.Length))
                    Console.WriteLine()
                    Console.WriteLine(title)
                    Console.WriteLine()
                    Console.ForegroundColor <- defColor
                    Console.WriteLine()
                    Console.WriteLine(gcInfo)
                    Console.ForegroundColor <- ConsoleColor.Yellow
                    Console.WriteLine(new String('*', gcInfo.Length))
                    Console.ForegroundColor <- ConsoleColor.Red
                    Console.WriteLine(new String('*', gcInfo.Length))
                    Console.ForegroundColor <- defColor

module PerfUtil =
    open System
    #nowarn "42"
    
    module PerTypes =
        open System
        open System.IO
        open System.Text

        /// An abstract implementation interface
        type ITestable =
            /// Implementation name.
            abstract Name : string

            /// Run before each test run
            abstract Init : unit -> unit
            /// Run after each test run
            abstract Fini : unit -> unit

        /// Represents a performance test for a given class of implementations.
        type PerfTest<'Testable when 'Testable :> ITestable> =
            {
                Id : string
                Repeat : int
                Test : 'Testable -> unit
            }

        type PerfTest =
            /// <summary>
            ///     Defines a new PerfTest instance
            /// </summary>
            /// <param name="testF">The test function.</param>
            /// <param name="id">Test id.</param>
            /// <param name="repeat">Number of repetitions.</param>
            static member Create<'Testable when 'Testable :> ITestable>(testF, ?id, ?repeat) : PerfTest<'Testable> =
                {
                    Test = testF
                    Id = match id with Some i -> i | None -> testF.GetType().Name
                    Repeat = defaultArg repeat 1
                }

        /// abstract performance tester
        [<AbstractClass>]
        type PerformanceTester<'Testable when 'Testable :> ITestable> () =

            /// The implementation under test.
            abstract TestedImplementation : 'Testable
            /// Run a performance test.
            abstract RunTest : PerfTest<'Testable> -> unit
            /// Get accumulated test results.
            abstract GetTestResults : unit -> TestSession list

            /// <summary>
            ///   Benchmarks given function.  
            /// </summary>
            /// <param name="testF">The test function.</param>
            /// <param name="id">Test id.</param>
            /// <param name="repeat">Number of repetitions.</param>
            member __.Run (testF : 'Testable -> unit, ?id, ?repeat) = 
                let test = PerfTest.Create(testF, ?id = id, ?repeat = repeat)
                __.RunTest test

        /// compares between two performance results
        and IPerformanceComparer =
            /// Decides if current performance is better or equivalent to the other/older performance.
            abstract IsBetterOrEquivalent : current:PerfResult -> other:PerfResult -> bool
            /// Returns a message based on comparison of the two benchmarks.
            abstract GetComparisonMessage : current:PerfResult -> other:PerfResult -> string

        /// Represents a collection of tests performed in a given run.
        and TestSession =
            {   
                Id : string
                Date : DateTime
                /// host id that performed given test.
                Hostname : string
                /// results indexed by test id
                Results : Map<string, PerfResult>
            }
        with
            member internal s.Append(br : PerfResult, ?overwrite) =
                let overwrite = defaultArg overwrite true
                if not overwrite && s.Results.ContainsKey br.TestId then
                    invalidOp <| sprintf "A test '%s' has already been recorded." br.TestId

                { s with Results = s.Results.Add(br.TestId, br) }

            static member internal Empty hostname (id : string) =
                {
                    Id = id
                    Hostname = hostname
                    Date = DateTime.Now
                    Results = Map.empty
                }

        /// Contains performance information
        and PerfResult =
            {
                /// Test identifier
                TestId : string
                /// Test session identifier
                SessionId : string
                /// Execution date
                Date : DateTime

                /// Catch potential error message
                Error : string option

                /// Number of times the test was run
                Repeat : int

                Elapsed : TimeSpan
                CpuTime : TimeSpan
                /// Garbage collect differential per generation
                GcDelta : int list
            }
        with
            override r.ToString () =
                let sb = new StringBuilder()
                sb.Append(sprintf "%s: Real: %O, CPU: %O" r.TestId r.Elapsed r.CpuTime) |> ignore
                r.GcDelta |> List.iteri (fun g i -> sb.Append(sprintf ", gen%d: %d" g i) |> ignore)
                sb.Append(sprintf ", Date: %O" r.Date) |> ignore
                sb.ToString()

            member r.HasFailed = r.Error.IsSome


        type PerformanceException (message : string, this : PerfResult, other : PerfResult) =
            inherit System.Exception(message)

            do assert(this.TestId = other.TestId)

            member __.TestId = this.TestId
            member __.CurrentTestResult = this
            member __.OtherTestResult = other

        /// indicates that given method is a performance test
        type PerfTestAttribute(repeat : int) =
            inherit System.Attribute()
            new () = new PerfTestAttribute(1)
            member __.Repeat = repeat

        type PerfUtil private () =
            static let mutable result = 
                let libPath = 
                    System.Reflection.Assembly.GetExecutingAssembly().Location 
                    |> Path.GetDirectoryName

                Path.Combine(libPath, "perfResults.xml")

            /// gets or sets the default persistence file used by the PastSessionComparer
            static member DefaultPersistenceFile
                with get () = result
                and set (path: string) =
                    if not <| File.Exists(Path.GetDirectoryName path) then
                        invalidOp <| sprintf "'%s' is not a valid path." path

                    lock result (fun () -> result <- path)
                    
    
    module internal Utils =
        open PerTypes
        let currentHost = System.Net.Dns.GetHostName()

        [<Literal>]
        let gcGenWeight = 10
        
        // computes a performance improvement factor out of two given timespans
        // add + 1L to eliminate the slim possibility of division by zero's and NaN's
        // ticks register large numbers so this shouldn't skew the final result significantly.
        let getTimeSpanRatio (this : TimeSpan) (that : TimeSpan) =
            float (decimal (that.Ticks + 1L) / decimal (this.Ticks + 1L))

        // computes a polynomial value out of gc garbage collection data
        // [ gen0 ; gen1 ; gen2 ] -> gen0 + 10 * gen1 + 10^2 * gen2
        let getSpace (r : PerfResult) = 
            r.GcDelta
            |> List.mapi (fun i g -> g * pown gcGenWeight i) 
            |> List.sum

        let getSpaceRatio (this : PerfResult) (that : PerfResult) =
            (float (getSpace that) + 0.1) / (float (getSpace this) + 0.1)

        // add single quotes if text contains whitespace
        let quoteText (text : string) =
            if text |> Seq.exists Char.IsWhiteSpace then
                sprintf "'%s'" text
            else
                text

        let defaultComparisonMessage (this : PerfResult) (other : PerfResult) =
            assert(this.TestId = other.TestId)

            match this.Error, other.Error with
            | Some e, _ ->
                sprintf "%s: %s failed with %O." this.TestId (quoteText this.SessionId) e
            | _, Some e ->
                sprintf "%s: %s failed with '%s'." other.TestId (quoteText other.SessionId) e
            | _ ->
                sprintf "%s: %s was %.2fx faster and %.2fx more memory efficient than %s."
                    (quoteText other.TestId)
                    (quoteText this.SessionId)
                    (getTimeSpanRatio this.Elapsed other.Elapsed)
                    (getSpaceRatio this other)
                    (quoteText other.SessionId)

        /// only returns items in enumeration that appear as duplicates
        let getDuplicates xs =
            xs
            |> Seq.groupBy id 
            |> Seq.choose (fun (id, vs) -> if Seq.length vs > 1 then Some id else None)


        //
        //  PerfTest activation code
        //

        open System.Reflection

        type MemberInfo with
            member m.TryGetAttribute<'T when 'T :> Attribute> () =
                match m.GetCustomAttributes(typeof<'T>, true) with
                | [||] -> None
                | attrs -> attrs.[0] :?> 'T |> Some

        type Delegate with
            static member Create<'T when 'T :> Delegate> (parentObj : obj, m : MethodInfo) =
                if m.IsStatic then
                    Delegate.CreateDelegate(typeof<'T>, m) :?> 'T
                else
                    Delegate.CreateDelegate(typeof<'T>, parentObj, m) :?> 'T


        type SynVoid =
            static member Swap(t : Type) =
                if t = typeof<Void> then typeof<SynVoid> else t

        type MethodWrapper =
            static member WrapUntyped<'Param> (parentObj : obj, m : MethodInfo) =
                typeof<MethodWrapper>
                    .GetMethod("Wrap", BindingFlags.NonPublic ||| BindingFlags.Static)
                    .MakeGenericMethod([| typeof<'Param> ; SynVoid.Swap m.ReturnType |])
                    .Invoke(null, [| parentObj ; box m |]) :?> 'Param -> unit

            static member Wrap<'Param, 'ReturnType> (parentObj, m : MethodInfo) =
                if typeof<'ReturnType> = typeof<SynVoid> then
                    let d = Delegate.Create<Action<'Param>>(parentObj, m)
                    d.Invoke
                else
                    let d = Delegate.Create<Func<'Param, 'ReturnType>> (parentObj, m)
                    d.Invoke >> ignore

        let getPerfTestsOfType<'Impl when 'Impl :> ITestable> ignoreAbstracts bindingFlags (t : Type) =
            let defBindings = BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.Instance
            let bindingFlags = defaultArg bindingFlags defBindings

            if t.IsGenericTypeDefinition then
                failwithf "Container type '%O' is generic." t

            let tryGetPerfTestAttr (m : MethodInfo) =
                match m.TryGetAttribute<PerfTestAttribute> () with
                | Some attr ->
                    if m.IsGenericMethodDefinition then
                        failwithf "Method '%O' marked with [<PerfTest>] attribute but is generic." m

                    match m.GetParameters() |> Array.map (fun p -> p.ParameterType) with
                    | [| param |] when param = typeof<'Impl> -> Some(m, attr)
                    | [| param |] when typeof<ITestable>.IsAssignableFrom(param) -> None
                    | _ -> 
                        failwithf "Method '%O' marked with [<PerfTest>] attribute but contains invalid parameters." m
                | None -> None

            let perfMethods = t.GetMethods(bindingFlags) |> Array.choose tryGetPerfTestAttr

            let requireInstance = perfMethods |> Array.exists (fun (m,_) -> not m.IsStatic)

            if ignoreAbstracts && t.IsAbstract && requireInstance then []
            else
                let instance = 
                    if requireInstance then
                        Activator.CreateInstance t
                    else
                        null

                let perfTestOfMethod (m : MethodInfo, attr : PerfTestAttribute) =
                    {
                        Id = sprintf "%s.%s" m.DeclaringType.Name m.Name
                        Repeat = attr.Repeat
                        Test = MethodWrapper.WrapUntyped<'Impl> (instance, m) 
                    }

                perfMethods |> Seq.map perfTestOfMethod |> Seq.toList
                
                
    open System
    open System.Reflection
    open Utils
    open PerTypes
    
    // benchmarking code, taken from FSI timer implementation

    type Benchmark private () =
            
        static let lockObj = box 42
        static let proc = System.Diagnostics.Process.GetCurrentProcess()
        static let numGC = System.GC.MaxGeneration

        /// <summary>Benchmarks a given computation.</summary>
        /// <param name="testF">Test function.</param>
        /// <param name="state">Input state to the test function.</param>
        /// <param name="repeat">Number of times to repeat the benchmark. Defaults to 1.</param>
        /// <param name="warmup">Perform a warmup run before attempting benchmark. Defaults to false.</param>
        /// <param name="sessionId">Test session identifier given to benchmark. Defaults to empty string.</param>
        /// <param name="testId">Test identifier given to benchmark. Defaults to empty string.</param>
        /// <param name="catchExceptions">Catches exceptions raised by the test function. Defaults to false.</param>
        static member Run<'State>(testF : 'State -> unit, state : 'State, ?repeat, ?warmup, ?sessionId, ?testId, ?catchExceptions) =
            let repeat = defaultArg repeat 1
            let catchExceptions = defaultArg catchExceptions false
            let warmup = defaultArg warmup false
            let testId = defaultArg testId ""
            let sessionId = defaultArg sessionId ""

            lock lockObj (fun () ->

            let stopwatch = new System.Diagnostics.Stopwatch()

            if warmup then
                try testF state
                with e when catchExceptions -> ()

            do 
                GC.Collect(3)
                GC.WaitForPendingFinalizers()
                GC.Collect(3)
                System.Threading.Thread.Sleep(100)


            let gcDelta = Array.zeroCreate<int> (numGC + 1)
            let inline computeGcDelta () =
                for i = 0 to numGC do
                    gcDelta.[i] <- System.GC.CollectionCount(i) - gcDelta.[i]

            do computeGcDelta ()
            let startTotal = proc.TotalProcessorTime
            let date = DateTime.Now
            stopwatch.Start()

            let error = 
                try 
                    for i = 1 to repeat do testF state 
                    None 
                with e when catchExceptions -> Some e.Message

            stopwatch.Stop()
            let total = proc.TotalProcessorTime - startTotal
            do computeGcDelta ()

            {
                Date = date
                TestId = testId
                SessionId = sessionId

                Error = error

                Repeat = repeat

                Elapsed = stopwatch.Elapsed
                CpuTime = total
                GcDelta = Array.toList gcDelta
            })
            

        /// <summary>Benchmarks a given computation.</summary>
        /// <param name="testF">Test function.</param>
        /// <param name="repeat">Number of times to repeat the benchmark. Defaults to 1.</param>
        /// <param name="warmup">Perform a warmup run before attempting benchmark. Defaults to false.</param>
        /// <param name="sessionId">Test session identifier given to benchmark. Defaults to empty string.</param>
        /// <param name="testId">Test identifier given to benchmark. Defaults to empty string.</param>
        /// <param name="catchExceptions">Catches exceptions raised by the test function. Defaults to false.</param>
        static member Run(testF : unit -> unit, ?repeat, ?warmup, ?sessionId, ?testId, ?catchExceptions) =
            Benchmark.Run(testF, (), ?repeat = repeat, ?sessionId = sessionId, ?warmup = warmup,
                                        ?testId = testId, ?catchExceptions = catchExceptions)

        /// <summary>Runs a given performance test.</summary>
        /// <param name="testF">Performance test.</param>
        /// <param name="impl">Implementation to run the performance test on.</param>
        /// <param name="warmup">Perform a warmup run before attempting benchmark. Defaults to false.</param>
        /// <param name="catchExceptions">Catches exceptions raised by the test function. Defaults to false.</param>
        /// <param name="sessionId">Test session identifier given to benchmark. Defaults to empty string.</param>
        /// <param name="testId">Test identifier given to benchmark. Defaults to empty string.</param>
        static member Run(perfTest : PerfTest<'Impl>, impl : 'Impl, ?warmup, ?catchExceptions, ?sessionId, ?testId) =
            try 
                do impl.Init()
                let testId = defaultArg testId perfTest.Id
                let sessionId = defaultArg sessionId impl.Name
                Benchmark.Run(perfTest.Test, impl, sessionId = sessionId, testId = testId, ?warmup = warmup,
                                                repeat = perfTest.Repeat, ?catchExceptions = catchExceptions)
            finally impl.Fini()
    
            
    let Run (action:System.Action) =
        Benchmark.Run (action.Invoke)                      