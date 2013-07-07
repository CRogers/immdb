module Node

open Crypto
open Network
open System.Net.Sockets
open Util

let nameLength = 128/8


type MsgType =
    | Connect = 0x0

type Peer = {
    Id: string
    IPEndPoint: IPEndPointC
}
with
    override this.ToString() = sprintf "%s || %s" this.Id <| this.IPEndPoint.ToString()

type Node = {
    Id: string;
    TcpManager: TcpManager
    mutable Peers: Map<string,Peer>
}
with
    override this.ToString() = sprintf "%s || %s" this.Id <| this.TcpManager.LocalEndPoint.ToString()

let nullNode = { Id = null; TcpManager = nullTcpManager; Peers = Map.empty }



let findPeer node id = Map.find id node.Peers

let sendBytesPeer node id bytes =
    let peer = findPeer node id
    sendMsg node.TcpManager peer.IPEndPoint bytes

let recvBytesPeer node id =
    let peer = findPeer node id
    recvMsg node.TcpManager peer.IPEndPoint

let sendMsgPeer node id (msgType:MsgType) bytes =
    let bs = Seq.toArray <| Seq.append [byte msgType] bytes
    sendBytesPeer node id bs

let recvMsgPeer node id = async {
    let! msg = recvBytesPeer node id
    let msgType = enum<MsgType>(int32 msg.[0])
    let data = arraySkip 1 msg
    return (msgType, data)
}



let connectBack node tcpm (client:TcpClient) remoteEndPoint = async {
    let node = !node

    // Get the connector's id
    let! msg = recvMsg tcpm remoteEndPoint
    let peerId = bytesToString msg

    let peer = { Id = peerId; IPEndPoint = remoteEndPoint }
    node.Peers <- node.Peers.Add(peerId, peer)

    // Send our id
    do! sendBytesPeer node peerId <| stringToBytes node.Id
}

let makeNode hostname port =
    let id = "IM:" + (bytesToBase64 <| randomBytes nameLength)
    // Hacky getting around needing the tcp manager to construct the node and the node to construct the tcpm
    let noder = ref <| nullNode

    let tcpm = Async.RunSynchronously <| makeTcpManager hostname port (connectBack noder)

    let node = { Id = id; TcpManager = tcpm; Peers = Map.empty }
    noder := node
    node

let connectToPeer node ipe = async {
    let man = node.TcpManager

    // Send our id
    do! sendMsg man ipe <| stringToBytes node.Id
    // Get their id
    let! resp = recvMsg man ipe
    let peerId = bytesToString resp

    let peer = { Id = peerId; IPEndPoint = ipe }
    node.Peers <- node.Peers.Add(peerId, peer)
}