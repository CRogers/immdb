module Network

open System
open System.Net
open System.Net.Sockets
open Util

let uncurry f (a,b) = f a b

let localhost = "127.0.0.1"

type IPEndPointC(ipe: IPEndPoint) =
    inherit IPEndPoint(ipe.Address, ipe.Port)
    let comp (this:IPEndPointC) (other:IPEndPointC) = 
        if this.Address.GetAddressBytes() < other.Address.GetAddressBytes() then -1
            elif this.Port = other.Port then 0
            else 1

    interface IComparable<IPEndPointC> with
        member this.CompareTo other = comp this other          

    interface IComparable with
        member this.CompareTo other = comp this (other :?> IPEndPointC)

type TcpManager = {
    LocalEndPoint: IPEndPoint
    TcpListener: TcpListener
    mutable TcpClients: Map<IPEndPointC,TcpClient>
}

let nullTcpManager = { LocalEndPoint = null; TcpListener = null; TcpClients = Map.empty }

let getIPEndPointFromHostname hostname port = async {
    let! addrs = Async.AwaitTask <| Dns.GetHostAddressesAsync(hostname)
    return IPEndPointC(new IPEndPoint(addrs.[0], port))
}

let makeTcpManager hostname port acceptClientFunc = async {
    let! localEndPoint = getIPEndPointFromHostname hostname port

    let tcpl = new TcpListener(localEndPoint)
    tcpl.Start()
    
    let tcpm = { LocalEndPoint = localEndPoint; TcpListener = tcpl; TcpClients = Map.empty }

    Async.Start <| async {
        while true do
            let client = tcpl.AcceptTcpClient()
            let remoteEndPoint = IPEndPointC(client.Client.RemoteEndPoint :?> IPEndPoint)
            tcpm.TcpClients <- tcpm.TcpClients.Add(remoteEndPoint, client)

            Async.Start <| acceptClientFunc tcpm client remoteEndPoint
    }

    return tcpm
}

/// Send a raw set of bytes to an IP endpoint.
let sendBytes tcpm ipe bytes =
    let client = match Map.tryFind ipe tcpm.TcpClients with
        | Some tcpc -> tcpc
        | None ->
            let c = new TcpClient()
            c.Connect(ipe)
            tcpm.TcpClients <- tcpm.TcpClients.Add(ipe, c)
            c

    let ns = client.GetStream()
    ns.AsyncWrite bytes

/// Recieve a number of bytes from an IP endpoint
let recvBytes tcpm ipe nbytes =
    let client = match Map.tryFind ipe tcpm.TcpClients with
        | Some tcpc -> tcpc
        | None -> raise <| Exception(sprintf "Cannot recieve message: %s is not connected to endpoint %s" (tcpm.LocalEndPoint.ToString()) (ipe.ToString()))

    let ns = client.GetStream()
    ns.AsyncRead nbytes

/// Send a set of bytes where the length is prefixed.
let sendMsg tcpm ipe bytes =
    let msg = Seq.append (intToBytes <| Seq.length bytes) bytes
    sendBytes tcpm ipe <| Seq.toArray msg

/// Recieve a length prefixed message and return the bit after the length.
let recvMsg tcpm ipe = async {
    let! lengthBytes = recvBytes tcpm ipe 4
    let length = bytesToInt lengthBytes
    return! recvBytes tcpm ipe length
}

