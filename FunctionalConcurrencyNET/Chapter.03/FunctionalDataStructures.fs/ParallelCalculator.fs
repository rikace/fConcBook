module ParallelCalculator

open System.Threading.Tasks

// Listing 3.23 Parallel Calculator
type Operation = Add | Sub | Mul | Div | Pow

and Calculator =
    | Value of double
    | Expr of Operation * Calculator * Calculator

let spawn (op:unit->double) = Task.Run(op)    //#A

let rec eval expr =
    match expr with                           //#B
    | Value(value) -> value                   //#C
    | Expr(op, lExpr, rExpr) ->               //#D
        let op1 = spawn(fun () -> eval lExpr) //#E
        let op2 = spawn(fun () -> eval rExpr) //#E
        let apply = Task.WhenAll([op1;op2])   //#F
        let lRes, rRes = apply.Result.[0], apply.Result.[1]
        match op with                         //#G
        | Add -> lRes + rRes
        | Sub -> lRes - rRes
        | Mul -> lRes * rRes
        | Div -> lRes / rRes
        | Pow -> System.Math.Pow(lRes, rRes)


let operations = // 2^10 / 2^9 + 2*2
    Expr(Add,
        Expr(Div,
            Expr(Pow, Value(2.0), Value(10.0)),
            Expr(Pow, Value(2.0), Value(9.0))),
        Expr(Mul, Value(2.0), Value(2.0)))

let value = eval operations