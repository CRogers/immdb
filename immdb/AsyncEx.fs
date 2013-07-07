module AsyncEx

open System.Threading.Tasks

let AwaitUnitTask (task:Task) =
    task.ContinueWith(fun _ -> ())
    |> Async.AwaitTask

let ParallelMap f =
    Seq.map f >> Async.Parallel

let RunParallelMap f = ParallelMap f >> Async.RunSynchronously