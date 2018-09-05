open System
// let binding
module binding =
    let myInt = 42
    let myFloat = 3.14
    let myString = "hello functional programming"
    let myFunction = fun number -> number * number

    // Create mutable types – mutable and ref
    let mutable myNumber = 42
    myNumber  <-  51

    let myRefVar = ref 42
    myRefVar := 53
    printfn "%d" !myRefVar

// Functions as first class types
module FunctionFirstClass =
    let square x = x * x
    let plusOne x = x + 1
    let isEven x = x % 2 = 0

    // Composition - Pipe and Composition operators
    let inline (|>) x f = f x
    let inline (>>) f g x = g(f x)

    let squarePlusOne x =  x |> square |> plusOne
    let plusOneIsEven = plusOne >> isEven

// Delegates
module Delegates =
    // type delegate typename = delegate of typeA -> typeB

    type MyDelegate = delegate of (int * int) -> int
    let add (a, b) = a + b
    let addDelegate = MyDelegate(add)
    let result = addDelegate.Invoke(33, 9)

// Comments
(* This is block comment *)
// Single line comments use a double forward slash
/// This comment can be used to generate documentation.

// Special String definition
module Strings =
    let verbatimHtml = @"<input type=\""submit\"" value=\""Submit\"">"
    let tripleHTML = """<input type="submit" value="Submit">"""

// Tuple
module Tuple =
    let tuple = (1, "Hello")
    let tripleTuple = ("one", "two", "three")

    let tupleStruct = struct (1, "Hello")

    let (a, b) = tuple
    let swap (a, b) = (b, a)

    let one = fst tuple
    let hello = snd tuple

// Record-Types
module RecordTypes =
    type Person = { FirstName : string; LastName : string; Age : int }
    let fred = { FirstName = "Fred"; LastName = "Flintstone"; Age = 42 }

    type Person with
        member this.FullName = sprintf "%s %s" this.FirstName this.LastName

    let olderFred = { fred with Age = fred.Age + 1 }

    [<Struct>]
    type Person_Struct = { FirstName : string; LastName : string; Age : int }

// Discriminated Unions
module Discriminated_Unions =
    type Suit = Hearts | Clubs | Diamonds | Spades

    type Rank =
            | Value of int
            | Ace
            | King
            | Queen
            | Jack
            static member GetAllRanks() =
                [ yield Ace
                  for i in 2 .. 10 do yield Value i
                  yield Jack
                  yield Queen
                  yield King ]

    type Card = { Suit:Suit; Rank:Rank }

    let fullDeck =
            [ for suit in [ Hearts; Diamonds; Clubs; Spades] do
                  for rank in Rank.GetAllRanks() do
                      yield { Suit=suit; Rank=rank } ]

// Pattern matching
module Pattern_matching =
    let fizzBuzz n =
        let divisibleBy m = n % m = 0
        match divisibleBy 3,divisibleBy 5 with
            | true, false -> "Fizz"
            | false, true -> "Buzz"
            | true, true -> "FizzBuzz"
            | false, false -> sprintf "%d" n

    let fizzBuzz' n =
        match n with
        | _ when (n % 15) = 0 -> "FizzBuzz"
        | _ when (n % 3) = 0 -> "Fizz"
        | _ when (n % 5) = 0 -> "Buzz"
        | _ -> sprintf "%d" n

    [1..20] |> List.iter(fun s -> printfn "%s" (fizzBuzz' s))

     //  Active patterns
    let (|DivisibleBy|_|) divideBy n =
       if n % divideBy = 0 then Some DivisibleBy else None


    let fizzBuzz'' n =
        match n with
        | DivisibleBy 3 & DivisibleBy 5 -> "FizzBuzz"
        | DivisibleBy 3 -> "Fizz"
        | DivisibleBy 5 -> "Buzz"
        | _ -> sprintf "%d" n

    [1..20] |> List.iter(fun s -> printfn "%s" (fizzBuzz'' s))

    let (|Fizz|Buzz|FizzBuzz|Val|) n =
        match n % 3, n % 5 with
        | 0, 0 -> FizzBuzz
        | 0, _ -> Fizz
        | _, 0 -> Buzz
        | _ -> Val n

// Arrays
module Arrays =
    let emptyArray1 = Array.empty
    let emptyArray2 = [| |]
    let arrayOfFiveElements = [| 1; 2; 3; 4; 5 |]
    let arrayFromTwoToTen= [| 2..10 |]
    let appendTwoArrays = emptyArray1 |> Array.append arrayFromTwoToTen
    let evenNumbers = arrayFromTwoToTen |> Array.filter(fun n -> n % 2 = 0)
    let squareNumbers = evenNumbers |> Array.map(fun n -> n * n)

    let arr = Array.init 10 (fun i -> i * i)
    arr.[1] <- 42
    arr.[7] <- 91

    let arrOfBytes = Array.create 42 0uy
    let arrOfSquare = Array.init 42 (fun i -> i * i)
    let arrOfIntegers = Array.zeroCreate<int> 42

// Sequences
module Sequences =
    let emptySeq = Seq.empty
    let seqFromTwoToFive = seq { yield 2; yield 3; yield 4; yield 5 }
    let seqOfFiveElements = seq { 1 .. 5 }
    let concatenateTwoSeqs = emptySeq |> Seq.append seqOfFiveElements
    let oddNumbers = seqFromTwoToFive |> Seq.filter(fun n -> n % 2 <> 0)
    let doubleNumbers = oddNumbers |> Seq.map(fun n -> n + n)

// Lists
module Lists =
    let emptyList1 = List.empty
    let emptyList2 = [ ]
    let listOfFiveElements = [ 1; 2; 3; 4; 5 ]
    let listFromTwoToTen = [ 2..10 ]
    let appendOneToEmptyList = 1::emptyList1
    let concatenateTwoLists = listOfFiveElements @ listFromTwoToTen
    let evenNumbers = listOfFiveElements |> List.filter(fun n -> n % 2 = 0)
    let squareNumbers = evenNumbers |> List.map(fun n -> n * n)

// Sets
module Sets =
    let emptySet = Set.empty<int>
    let setWithOneItem = emptySet.Add 8
    let setFromList = [ 1..10 ] |> Set.ofList

// Maps
module Maps =
    let emptyMap = Map.empty<int, string>
    let mapWithOneItem = emptyMap.Add(42, "the answer to the meaning of life")
    let mapFromList = [ (1, "Hello"), (2, "World") ] |> Map.ofSeq

// Loops
module Loops =
    let mutable a = 10
    while (a < 20) do
       printfn "value of a: %d" a
       a <- a + 1

    for i = 1 to 10 do
        printf "%d " i

    for i in [1..10] do
       printfn "%d" i

// Class and inheritance
module Class_and_inheritance =
    type Person(firstName, lastName, age) =
        member this.FirstName = firstName
        member this.LastName = lastName
        member this.Age = age

        member this.UpdateAge(n:int) =
            Person(firstName, lastName, age + n)

        override this.ToString() =
            sprintf "%s %s" firstName lastName


    type Student(firstName, lastName, age, grade) =
        inherit Person(firstName, lastName, age)

        member this.Grade = grade

// Abstract classes and inheritance
module Abstract_class_and_inheritance =
    [<AbstractClass>]
    type Shape(weight :float, height :float) =
        member this.Weight = weight
        member this.Height = height

        abstract member Area : unit -> float
        default this.Area() = weight * height

    type Rectangle(weight :float, height :float) =
        inherit Shape(weight, height)

    type Circle(radius :float) =
        inherit Shape(radius, radius)
        override this.Area() = radius * radius * Math.PI

// Interfaces
module Interfaces =
    type IPerson =
       abstract FirstName : string
       abstract LastName : string
       abstract FullName : unit -> string

    type Person(firstName : string, lastName : string) =
        interface IPerson with
            member this.FirstName = firstName
            member this.LastName = lastName
            member this.FullName() = sprintf "%s %s" firstName lastName

    let fred = Person("Fred", "Flintstone")

    (fred :> IPerson).FullName()

// Object expressions
module Object_expressions =
    let print color =
        let current = Console.ForegroundColor
        Console.ForegroundColor <- color
        {   new IDisposable with
                 member x.Dispose() =
                    Console.ForegroundColor <- current
        }

    using(print ConsoleColor.Red) (fun _ -> printf "Hello in red!!")
    using(print ConsoleColor.Blue) (fun _ -> printf "Hello in blue!!")

// Casting
module Castings =
    open Interfaces

    let testPersonType (o:obj) =
           match o with
           | :? IPerson as person -> printfn "this object is an IPerson"
           | _ -> printfn "this is not an IPerson"

// Units of Measure
module Measure =
    [<Measure>]
    type m

    [<Measure>]
    type sec

    let distance = 25.0<m>
    let time = 10.0<sec>

    let speed = distance / time


[<EntryPoint>]
let main argv =
    printfn "%A" argv
    0