namespace GameOfLife

open System
open System.Windows
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Threading.Tasks
open System.Collections.Generic
open System.Linq

type Agent<'a> = MailboxProcessor<'a>

module GameOfLifeAgent =
    let alive = false

    let size = 60
    type Grid = {Width:int; Height:int}
    let gridProduct = size * size
    let grid = {Width=size;Height=size}

    type Location = {x:int; y:int}

    type IDictionary<'a,'b> with
        member this.find k = this.[k]

    type UpdateView =
        | Reset
        | Update of bool * Location

    let image = Controls.Image(Stretch=Stretch.Uniform)

    let applyGrid f =
        for x = 0 to grid.Width - 1 do
            for y = 0 to grid.Height - 1 do f x y

    let createImage pixels = BitmapSource.Create(grid.Width, grid.Height, 96., 96., PixelFormats.Gray8, null, pixels, size)

    let updateAgent (ctx:System.Windows.Threading.Dispatcher) =
        let pixels = Array.create (size*size) 0uy
        let agent = new Agent<UpdateView>(fun inbox ->
            let rec loop agentStates = async {
                let! msg = inbox.Receive()
                match msg with
                | UpdateView.Reset -> return! loop (Dictionary<Location, bool>(HashIdentity.Structural))
                | Update(alive, location) ->
                    agentStates.[location] <- alive
                    if agentStates.Count = gridProduct then
                        applyGrid (fun x y ->
                                match agentStates.TryGetValue({x=x;y=y}) with
                                | true, s when s = true ->
                                    pixels.[x+y*size] <- byte 128
                                | _ -> pixels.[x+y*size] <- byte 0)
                        ctx.Invoke(fun () -> image.Source <- createImage pixels)
                    return! loop agentStates
            }
            loop (Dictionary<Location, bool>(HashIdentity.Structural)))
        agent

    type CellMessage =
        | NeighbourState of Cell * bool
        | State of Cell
        | Neighbours of Cell list
        | Reset

    and Cell(location, alive, updateAgent:Agent<_>) as this =
        let hashCode = hash location
        let neighbourStates = Dictionary<Cell, bool>()
        let agentCell =
            Agent<CellMessage>.Start(fun inbox ->
                let rec loop state = async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Reset ->
                        state.neighbours |> Seq.iter(fun cell -> cell.Send(State(this)))
                        neighbourStates.Clear()
                        return! loop { state with wasAlive=state.isAlive }
                    | Neighbours(neighbours) -> return! loop { state with neighbours=neighbours }
                    | State(c) -> c.Send(NeighbourState(this, state.wasAlive))
                                  return! loop state
                    | NeighbourState(cell, alive) ->
                        neighbourStates.[cell] <- alive
                        if neighbourStates.Count = 8 then
                            let aliveState =
                                match neighbourStates |> Seq.filter(fun (KeyValue(_,v)) -> v) |> Seq.length with
                                | a when a > 3  || a < 2 -> false
                                | 3 -> true
                                | _ -> state.isAlive
                            updateAgent.Post(UpdateView.Update(aliveState, location))
                            return! loop { state with isAlive = aliveState }
                        else return! loop state
                }
                loop (State.create alive ))

        member this.location = location
        member this.Send = agentCell.Post

        override this.Equals(o) =
            match o with
            | :? Cell as cell -> this.location = cell.location
            | _ -> false
        override this.GetHashCode() = hashCode

        interface IComparable<Cell> with
            member this.CompareTo(other) = compare this.location other.location
        interface IComparable with
            member this.CompareTo(o) =
              match o with
              | :? Cell as other -> (this :> IComparable<_>).CompareTo other
              | _ -> 1

    and State =
        {
            neighbours:Cell list
            wasAlive:bool
            isAlive:bool
        }
        with static member create isAlive =
                { neighbours=[];isAlive=isAlive; wasAlive=false; }

    let getRandomBool =
        let random = Random(int System.DateTime.Now.Ticks)
        fun () -> random.Next() % 2 = 0

    let run(ctx) =
        let updateAgent = updateAgent ctx

        let cells = seq {
            for x = 0 to grid.Width - 1 do
                for y = 0 to grid.Height - 1 do
                    yield (x,y), Cell({x=x;y=y}, alive=getRandomBool(), updateAgent=updateAgent) } |> dict

        let neighbours (location:Location) = seq {
            for x = location.x - 1 to location.x + 1 do
                for y = location.y - 1 to location.y + 1 do
                    if x <> location.x || y <> location.y then
                        yield cells.find ((x + grid.Width) % grid.Width, (y + grid.Height) % grid.Height) }

        applyGrid (fun x y ->
                let agent = cells.find (x,y)
                let neighbours = neighbours {x=x;y=y} |> Seq.toList
                agent.Send(Neighbours(neighbours)))

        let updateView() =
           updateAgent.Post(UpdateView.Reset)
           cells.Values.AsParallel().ForAll(fun cell -> cell.Send(Reset))

        do updateAgent.Start()

        let timer = new System.Timers.Timer(100.)
        let dispose = timer.Elapsed |> Observable.subscribe(fun _ -> updateView())
        timer.Start()
        dispose
