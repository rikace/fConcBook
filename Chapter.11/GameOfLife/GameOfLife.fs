module GameOfLife

open System
open System.Windows
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Threading
open System.Collections.Generic
open System.Linq

type Agent<'a> = MailboxProcessor<'a>
type Grid = { Width:int; Height:int }

[<Struct>]
type Location = {x:int; y:int}

[<Struct>]
type UpdateView =
    | Update of bool * Location * CellAgent

//Listing 11.9 Game of Life with MailboxProcessor as cells
and CellMessage =
    | NeighborState of cell:CellAgent * isalive:bool
    | State of cellstate:CellAgent
    | Neighbors of cells:CellAgent list
    | ResetCell    // #A

and State =
    {   neighbors:CellAgent list
        wasAlive:bool
        isAlive:bool }    // #B
    static member createDefault isAlive =
        { neighbors=[]; isAlive=isAlive; wasAlive=false; }

and CellAgent(location, alive, updateAgent:Agent<_>) as this =
    let neighborStates = Dictionary<CellAgent, bool>()  // #C
    let agentCell =
        Agent<CellMessage>.Start(fun inbox ->
            let rec loop state = async {
                let! msg = inbox.Receive()
                match msg with
                | ResetCell ->
                    state.neighbors
                    |> Seq.iter(fun cell -> cell.Send(State(this)))  // #D
                    neighborStates.Clear()
                    return! loop { state with wasAlive=state.isAlive } // #E
                | Neighbors(neighbors) ->
                    return! loop { state with neighbors=neighbors } // #E
                | State(c) ->
                    c.Send(NeighborState(this, state.wasAlive))
                    return! loop state
                | NeighborState(cell, alive) ->
                    neighborStates.[cell] <- alive
                    if neighborStates.Count = 8 then    // #F
                        let aliveState =
                            let numberOfneighborAlive =
                                neighborStates
                                |> Seq.filter(fun (KeyValue(_,v)) -> v) // #G
                                |> Seq.length
                            match numberOfneighborAlive with    // #G
                            | a when a > 3  || a < 2 -> false
                            | 3 -> true
                            | _ -> state.isAlive
                        updateAgent.Post(Update(aliveState, location, this)) // #H
                        return! loop { state with isAlive = aliveState }
                    else return! loop state }
            loop (State.createDefault alive ))

    member this.Send(msg) = agentCell.Post msg


let image = Controls.Image(Stretch=Stretch.Uniform)

let createImage (grid:Grid) (pixels:Array) =
    BitmapSource.Create(grid.Width, grid.Height, 96., 96., PixelFormats.Gray8, null, pixels, grid.Width)


//Listing 11.10 MailboxProcessor updateAgent that refreshes the WPF UI in real-time
let updateAgent grid (ctx: SynchronizationContext) = //#A
    let gridProduct = grid.Width * grid.Height
    let pixels = Array.zeroCreate<byte> (gridProduct)  //#B
    Agent<UpdateView>.Start(fun inbox ->
        let agentStates = Dictionary<Location, bool>(HashIdentity.Structural) //#C
        let rec loop () = async {
            let! msg = inbox.Receive()
            match msg with
            | Update(alive, location, agent) ->           //#D
                agentStates.[location] <- alive    //#D
                agent.Send(ResetCell)
                if agentStates.Count = gridProduct then    //#E
                    agentStates.AsParallel().ForAll(fun s ->
                        pixels.[s.Key.x+s.Key.y*grid.Width]
                            <- if s.Value then 128uy else 0uy //#F
                    )
                    do! Async.SwitchToContext ctx         //#G
                    image.Source <- createImage grid pixels    //#G
                    do! Async.SwitchToThreadPool()        //#G
                    agentStates.Clear()
                return! loop()
            }
        loop ())


let getRandomBool =
    let random = Random(int System.DateTime.Now.Ticks)
    fun () -> random.Next() % 2 = 0

//Listing 11.11 Creating the Game of Life grid and starting the timer for the refresh
let run (ctx:SynchronizationContext) =

    let size = 100    // #A
    let grid = { Width= size; Height=size}    // #B
    let updateAgent = updateAgent grid ctx

    let cells = seq { for x = 0 to grid.Width - 1 do
                         for y = 0 to grid.Height - 1 do    // #C
                            let agent = CellAgent({x=x;y=y},
                                            alive=getRandomBool(),
                                            updateAgent=updateAgent)
                            yield (x,y), agent  } |> dict

    let neighbors (x', y') =
        seq {
          for x = x' - 1 to x' + 1 do
            for y = y' - 1 to y' + 1 do
              if x <> x' || y <> y' then
                 yield cells.[(x + grid.Width) % grid.Width,
                              (y + grid.Height) % grid.Height]
        } |> Seq.toList

    cells.AsParallel().ForAll(fun pair -> //#D
        let cell = pair.Value
        let neighbors = neighbors pair.Key
        cell.Send(Neighbors(neighbors))  // #D
        cell.Send(ResetCell)             // #D
    )