### Build Status

* Windows [![Build status](https://ci.appveyor.com/api/projects/status/uq2bru4oqhixeipg?svg=true)](https://ci.appveyor.com/project/rikace/fconcbook)



# Important
## if there are problems with the build, please look into this ([issue here](https://github.com/Microsoft/visualfsharp/issues/5576))


# Concurrency in .NET 
## Modern patterns of concurrent and parallel programming 

This solution is complementary to the book ([Concurrency in .NET](https://www.manning.com/books/concurrency-in-dot-net)), which provides an introduction to functional concurrent programming concepts, and the skills you need in order to understand the functional aspects of writing multithreaded programs.
Chapters 4 to 12 dive into the different concurrent programming models of the functional paradigm. These chapters explore subjects such as the Task-Parallel Library, implementing parallel patterns such as Fork/Join, divide-and-conquer and Map-Reduce.  Also discussed is declarative composition, high level abstraction in asynchronous operations, the agent programming model, and the message passing semantic.
Then chapters 13 and 14 aim to exploit and put in practice all the functional concurrent programming techniques learned during the previous chapters. Chapter 13 contains a set of recipes to solve common parallel issues. Chapter 14 implements a full application client side (mobile iOS and windows WPF) and server side for real time stock market operations.

**Important to run the examples**

To runs the examples you need Visual Studio 2017 ([Download here](https://www.visualstudio.com)) and .NET Framework 4.7 ([Download here](https://www.microsoft.com/en-us/download/details.aspx?id=55170)). The examples in the code leverages the new language features that compile only with Visual Studio 2017.

**A compatible version of the code that runs on .NET Core is in progress and it will be released soon.**

Here description of the source code by chapter:

- **Chapter 1** exploit different implementation of **QuickSort** algorithm to highlight the main foundations and purposes behind concurrent programming, and the reasons for using functional programming to write multithreaded applications. The code examples are both in C# and F#.

- **Chapter 2** explores several functional programming techniques to improve the performance of a multithreaded application. The purpose of this chapter is to provide concepts used during the rest of the book, and to become familiar with powerful ideas originated from the functional paradigm. These functional techniques are **Closure, Composition, Concurrent Speculation, Laziness, Memoization**. The code examples are both in C# and F#.

- **Chapter 3** provides an overview of the functional concept of immutability. It explains how it is used to write predictable and correct concurrent programs, and how it is applied to implement and use functional data structures, which are intrinsically thread safe. The code examples include how to **implement a functional data-structure List, a Lazy-List, B-Tree, how to implement an optimized tail recursive function (TCO), and how to parallelize a recursive function using divide-conquer technique**. The code examples are both in C# and F#.

- **Chapter 4** covers the basics of processing a large amount of data in parallel, including patterns such as Fork/Join. The code example in this chapter include **parallel sum of Array, parallel (and memory optimized) Mandelbrot, parallel calculation of prime number and mode.** The code examples are both in C# and F#.

- **Chapter 5** introduces more advanced techniques for parallel processing massive data, such as aggregating and reducing data in parallel and implementing a parallel Map-Reduce pattern. The code example in this chapter include ** parallel K-Menas, different implementations of parallel Map-Reduce and parallel Reducer.** The code examples are both in C# and F#.
	
- **Chapter 6** provides details of the functional techniques to process real-time stream of events (data), leveraging functional higher order operators, with .NET Reactive Extensions, to compose asynchronous event combinators. The techniques learned are used to implement a concurrent friendly and reactive publisher-subscriber pattern. This chapter includes examples for **real time Twitter - stream processing (sentiment analysis) and a custom reactive publisher/subscriber.** The code examples are both in C# and F#.

- **Chapter 7** explains the Task-Based programming model applied to functional programming to implement concurrent operations using the Monadic pattern based on continuation passing style. This technique is then used to build a concurrent and functional based pipeline. This chapter includes examples for **image (face from picture) recognition, image processing in parallel, implement and exploit a parallel functional pipeline**. The code examples are both in C# and F#.

- **Chapter 8** concentrates on the Asynchronous programming model to implement unbounded parallel computations.  This chapter includes examples for **composing and run in parallel multiple asynchronous operations, and downloading and processing stock-tickers.** The code examples are both in C# and F#.

- **Chapter 9** focuses on the Asynchronous Workflow, explaining how the deferred and explicit evaluation of this model permits higher compositional semantic. Then, explains how to implement custom computation expression to raise the level of abstraction resulting in a declarative programming style.
This chapter also examines error handling and compositional techniques for asynchronous operations. 
This chapter includes examples for **control the degree of asynchronous parallelism, composing asynchronous operations, and download and process data from Azure.** The code examples are both in C# and F#.

- **Chapter 10** wraps up the previous chapters 8 & 9, and it culminates in implementing combinators and patterns such as Functor, Monad and Applicative to compose and run multiple asynchronous operations and handle errors, while avoiding side effects. This chapter includes multiple helper functions (combinators) that are used to refactor in a more idiomatic (functional) style the code from the previous chapters 8 & 9. The code examples are both in C# and F#.

- **Chapter 11** delves into reactive programming using the message passing programming model.  The concept of natural isolation as complemental technique with immutability for building concurrent programs will be covered. This chapter focuses on the F# MailboxProcessor for distributing parallel work using the agent model and the share-nothing approach. This chapter includes examples for **implementing a Game-of-Life using agents, an asynchronous cache agent that integrates wit I/O operations, and a custom parallel worker based on agents to control the level of parallelism to run multiple operations.** The code examples are in F#.

- **Chapter 12** explains the agent programming model using the .NET Task-Parallel Library DataFlow. This chapter explain how to implement both a stateless and stateful agent using C# and run multiple computations in parallel that communicate with each other using (passing) messages in a pipeline style. This chapter includes examples to **compress and encrypt (and vice-versa) in parallel a large file, and a parallel words counter agent-pipeline.** The code examples are in C#.
 
- **Chapter 13** contains a set of reusable and useful recipes to solve complex concurrent issues based on real World experiences. The recipes leverage the functional patterns absorbed during the book. The recipes implemented in this chapter are:
	- **AgentMultipleReadsOneWrite** : agent that allows the asynchronous access of multiple reads or one write to shared resources 
	- **AsyncObjectPool** : asynchronous agent to reuse objects instantiated for memory optimization.
	- **Channel** : agent to emulate the channel based concurrent model - [Communication sequential process CSP](https://en.wikipedia.org/wiki/Communicating_sequential_processes).
	- **EventAggregator** : multipurpose event types aggregator.
	- **ForkJoin** : reusable high performant fork/join extension.
	- **GraphTasks** : component used to run in parallel multiple dependent operations respecting the order (dependencies).
	- **ParallelFilterMap** : reusable high performant filter-map function combination extension.
	- **ReactiveNetworkStream** : server/client sockets based application to send and process real time stock tickers. This application is implemented using Reactive Extensions.
	- **RxCustomScheduler** : Custom and reusable Reactive Extensions scheduler to control the degree of parallelism 
	- **TamingAndComposingAsyncOps** : agent to run asynchronous operations in parallel with a specific degree of parallelism.
	- **Kleisli** : compose monadic types. In this example, the Kleisli operator is used to compose multiple agents in a pipeline.
	- **ThreadSafeRandom** : concurrent random number generator.

- **Chapter 14** is a full application designed and implemented using the functional concurrent patterns and techniques learned in the previous chapters. This chapter implements a highly scalable and responsive server application, and a reactive client side program. Two versions are presented, one using Xamarin Visual Studio for an iOS (iPad) based program, and one using WPF. The server side application uses a combination of different programming models, such as asynchronous, agent based and reactive to ensure maximum scalability. To run this application you can either use the WPF version of the client side or the iOS version using Xamarin for Visual Studio. For the latter, you need to have installed Xamarin for visual studio, you can follow the direction [here - link](https://developer.xamarin.com/guides/cross-platform/getting_started/installation/windows/)
	- **Note** : the StockTicker.Core has an embedded resource for Xamarin forms, which requires to run Visual Studio in admin mode 
