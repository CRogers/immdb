module Program

open Network
open Node
open System.Text
open System.Threading

[<EntryPoint>]
let main argv =
    let msgRecved msg = printfn "Message Recieved: %s" <| Encoding.ASCII.GetString(msg)
    
    Async.RunSynchronously <| async {

        let a = makeNode localhost 9090
        let b = makeNode localhost 9091

        let! [|aep;bep|] = AsyncEx.ParallelMap (getIPEndPointFromHostname localhost) [9090; 9091]

        do! connectToPeer a bep
        do! connectToPeer b aep
    }   

    0
