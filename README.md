# Concurrency in .NET 
## Modern patterns of concurrent and parallel programming 

[ ] Tools and framwerok needed
[ ] What to install for ch 14 
[ ] Next cominh .NET core 



Part 1 provides an introduction to functional concurrent programming concepts, and the skills you need in order to understand the functional aspects of writing multithreaded programs.

- Chapter 1 highlights the main foundations and purposes behind concurrent programming, and the reasons for using functional programming to write multithreaded applications.
	* QuickSort

- Chapter 2 explores several functional programming techniques to improve the performance of a multithreaded application. The purpose of this chapter is to provide concepts used during the rest of the book, and to become familiar with powerful ideas originated from the functional paradigm.
	* FP techniques Closure, Composition, Laziness, memoization
			

- Chapter 3 provides an overview of the functional concept of immutability. It explains how it is used to write predictable and correct concurrent programs, and how it is applied to implement and use functional data structures, which are intrinsically thread safe.
	* Lazy List, BTree, TCO, 


Part 2 dives into the different concurrent programming models of the functional paradigm.  We will explore subjects such as the Task-Parallel Library, implementing parallel patterns such as Fork/Join, divide-and-conquer and Map-Reduce.  Also discussed is declarative composition, high level abstraction in asynchronous operations, the agent programming model, and the message passing semantic.
- Chapter 4 covers the basics of processing a large amount of data in parallel, including patterns such as Fork/Join.
	* Mandelbrot, Parallel Sum of Prime number (emberasily parallel)
	
- Chapter 5 introduces more advanced techniques for parallel processing massive data, such as aggregating and reducing data in parallel and implementing a parallel Map-Reduce pattern.
	* KMenas, MapReduce, Paralle Reducer , PLINQ
	
- Chapter 6 provides details of the functional techniques to process real-time stream of events (data), leveraging functional higher order operators, with .NET Reactive Extensions, to compose asynchronous event combinators. The techniques learned are used to implement a concurrent friendly and reactive publisher-subscriber pattern.
	* Reactive Extensions composition, Twitter - stream processing realtime

- Chapter 7 explains the Task-Based programming model applied to functional programming to implement concurrent operations using the Monadic pattern based on continuation passing style. This technique is then used to build a concurrent and functional based pipeline. 
- Chapter 8 concentrates on the C# Asynchronous programming model to implement unbounded parallel computations. This chapter also examines error handling and compositional techniques for asynchronous operations.
- Chapter 9 focuses on the F# Asynchronous Workflow, explaining how the deferred and explicit evaluation of this model permits higher compositional semantic. Then, explains how to implement custom computation expression to raise the level of abstraction resulting in a declarative programming style.
- Chapter 10 wraps up the previous chapters and culminates in implementing combinators and patterns such as Functor, Monad and Applicative to compose and run multiple asynchronous operations and handle errors, while avoiding side effects.
- Chapter 11 delves into reactive programming using the message passing programming model.  The concept of natural isolation as complemental technique with immutability for building concurrent programs will be covered. This chapter focuses on the F# MailboxProcessor for distributing parallel work using the agent model and the share-nothing approach.
- Chapter 12 explains the agent programming model using the .NET Task-Parallel Library DataFlow with examples in C#.  You will implement both a stateless and statefull agent using C# and run multiple computations in parallel that communicate with each other using (passing) messages in a pipeline style

Part 3 aims to exploit and put in practice all the functional concurrent programming techniques learned during the previous chapters. 
- Chapter 13 contains a set of reusable and useful recipes to solve complex concurrent issues based on real World experiences. The recipes leverage the functional patterns absorbed during the book.
- Chapter 14 is a full application designed and implemented using the functional concurrent patterns and techniques learned in the book. You will build a highly scalable and responsive server application, and a reactive client side program. Two versions are presented, one using Xamarin Visual Studio for an iOS (iPad) based program, and one using WPF. The server side application uses a combination of different programming models, such as asynchronous, agent based and reactive to ensure maximum scalability.


