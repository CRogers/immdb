module Program

open Network
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading

[<EntryPoint>]
let main argv =
    let msgRecved msg = printfn "Message Recieved: %s" <| Encoding.ASCII.GetString(msg)

    Async.RunSynchronously <| async {
        let ports = [9090; 9091; 9092]

        let! [|a;b;c|] = AsyncEx.ParallelMap (makeTcpManager localhost) ports
        let! [|aep;bep;cep|] = AsyncEx.ParallelMap (getIPEndPointFromHostname localhost) ports

        do! sendMsg a bep (Encoding.ASCII.GetBytes "abcd")
        Thread.Sleep 500
        do! sendMsg b cep (Encoding.ASCII.GetBytes "efgh")
        Thread.Sleep 500

        let! msg1 = recvMsg b aep 4
        let! msg2 = recvMsg c bep 4

        do! sendMsg c aep <| Encoding.ASCII.GetBytes "zxyu"
        let! msg3 = recvMsg a cep 4

        msgRecved msg1
        msgRecved msg2
        msgRecved msg3
    }

    0
