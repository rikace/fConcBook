namespace GameOfLife

module Game =
        
    open System
    open System.Collections.Generic
    open System.Linq
    open Newtonsoft.Json
    open WebSocketMiddleware
    
    type Agent<'a> = MailboxProcessor<'a>
    type Grid = { Width:int; Height:int }

    [<Struct>]
    type Location = {x:int; y:int}
        
    [<Serializable>]
    type CellState(x : int,y : int, isAlive : bool) =
        member __.X = x
        member __.Y = y
        member __.IsAlive = isAlive
        
    [<Struct>]
    type UpdateView =
        | Update of bool * Location * CellAgent

    //Listing 11.9 Game of Life with MailboxProcessor as cells
    and CellMessage =
        | NeighborState of cell:CellAgent * isAlive:bool
        | State of cellState:CellAgent
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

    //Listing 11.10 MailboxProcessor updateAgent that refreshes the WPF UI in real-time
    let updateAgent grid cellSize = //#A
        let cellsCount = (grid.Width * grid.Height) / (cellSize * cellSize)
        Agent<UpdateView>.Start(fun inbox ->
            let agentStates = Dictionary<Location, bool>(HashIdentity.Structural) //#C
            let rec loop () = async {
                let! msg = inbox.Receive()
                match msg with
                | Update(alive, location, agent) ->           //#D
                    agentStates.[location] <- alive    //#D
                    agent.Send(ResetCell)                    
                    if agentStates.Count = cellsCount then  agentStates.Clear()  //#E
                    else 
                        // do! Async.SwitchToContext ctx         //#G                        
                        let cellState = CellState(location.x, location.y, alive)
                        let cellStateJson = JsonConvert.SerializeObject(cellState)
                        Middleware.sendMessageToSockets cellStateJson
                        // do! Async.SwitchToThreadPool()        //#G
                    return! loop()
                }
            loop ())


    let getRandomBool =
        let random = Random(int System.DateTime.Now.Ticks)
        fun () -> random.Next() % 2 = 0

    //Listing 11.11 Creating the Game of Life grid and starting the timer for the refresh
    let run () =

        // the size matches the Html Canvas Size and the cell drawn size 
        let gridSize = 1000    // #A
        let cellSize = 25
        
        let grid = { Width= gridSize; Height=gridSize}    // #B
        let updateAgent = updateAgent grid cellSize //ctx

        let cells = seq { for x in 0 .. cellSize .. grid.Width - cellSize do
                             for y in 0 .. cellSize .. grid.Height - cellSize do    // #C
                                let agent = CellAgent({x=x;y=y},
                                                alive=getRandomBool(),
                                                updateAgent=updateAgent)
                                yield (x,y), agent  } |> dict

        let neighbors (x', y') =
            seq {
              
              for x in (x' - cellSize) .. cellSize .. (x' + cellSize) do
                for y in (y' - cellSize) .. cellSize .. (y' + cellSize) do
                  if (x >= 0 && y >= 0 && x' >= 0 && y' >= 0)   
                     && (x <> x' || y <> y') then
                     yield cells.[(x + grid.Width) % grid.Width,
                                  (y + grid.Height) % grid.Height]
            } |> Seq.toList
 
        cells.AsParallel().ForAll(fun pair -> //#D
            let cell = pair.Value
            let neighbors = neighbors pair.Key
            cell.Send(Neighbors(neighbors))  // #D
            cell.Send(ResetCell)             // #D
        )
