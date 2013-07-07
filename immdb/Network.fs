module Network

open System
open System.Net
open System.Net.Sockets

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

let getIPEndPointFromHostname hostname port = async {
    let! addrs = Async.AwaitTask <| Dns.GetHostAddressesAsync(hostname)
    return IPEndPointC(new IPEndPoint(addrs.[0], port))
}

let makeTcpManager hostname port = async {
    let! localEndPoint = getIPEndPointFromHostname hostname port

    let tcpl = new TcpListener(localEndPoint)
    tcpl.Start()
    
    let tcps = { LocalEndPoint = localEndPoint; TcpListener = tcpl; TcpClients = Map.empty }

    Async.Start <| async {
        while true do
            let client = tcpl.AcceptTcpClient()
            let remoteEndPoint = IPEndPointC(client.Client.RemoteEndPoint :?> IPEndPoint)
            tcps.TcpClients <- tcps.TcpClients.Add(remoteEndPoint, client)
    }

    return tcps
}

let sendMsg tcps ipe bytes =
    let client = match Map.tryFind ipe tcps.TcpClients with
        | Some tcpc -> tcpc
        | None ->
            let c = new TcpClient()
            c.Connect(ipe)
            c

    let ns = client.GetStream()
    ns.AsyncWrite(Seq.toArray bytes)

let recvMsg tcps ipe nbytes =
    let client = match Map.tryFind ipe tcps.TcpClients with
        | Some tcpc -> tcpc
        | None -> raise <| Exception(sprintf "Cannot recieve message: %s is not connected to endpoint %s" (tcps.LocalEndPoint.ToString()) (ipe.ToString()))

    let ns = client.GetStream()
    ns.AsyncRead(nbytes)

