module TCO_CPS


// Listing 3.19 Tail recursive implementation of factorial in F#
let rec factorialTCO (n:int) (acc:int) =
    if n <= 1 then acc
    else factorialTCO (n-1) (acc * n) //#A

let factorial n = factorialTCO n 1


// Listing 3.21 Recursive implementation of factorial using CPS
let rec factorialCPS x continuation =
    if x <= 1 then continuation()
    else factorialCPS (x - 1) (fun () -> x * continuation())

let result = factorialCPS 4 (fun () -> 1) //#A
