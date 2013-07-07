module Program

open Logging
open Network
open Node
open System
open System.Text
open System.Threading
open Util

[<EntryPoint>]
let main argv =
    let msgRecved msg = printfn "Message Recieved: %s" <| Encoding.ASCII.GetString(msg)
    
    Async.RunSynchronously <| async {

        let a = makeNode localhost 9090
        let b = makeNode localhost 9091

        let! [|aep;bep|] = AsyncEx.ParallelMap (getIPEndPointFromHostname localhost) [9090; 9091]

        do! connectToPeer a bep
        do! connectToPeer b aep

        Logger.Log LogLevel.Network "Send test"
        
        do! sendMsgPeer b a.Id MsgType.Connect (stringToBytes "Hello, World!")
        let! msgType, data = recvMsgPeer a b.Id        

        printfn "%d:%s" (int msgType) (bytesToString data)
    }   

    0
