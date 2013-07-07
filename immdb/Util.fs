module Util

open System
open System.Text

let bytesToBase64 = Convert.ToBase64String
let base64ToBytes = Convert.FromBase64String

let longToBytes (i:int64) = BitConverter.GetBytes i

let intToBytes (i:int32) = BitConverter.GetBytes i
let bytesToInt bs = BitConverter.ToInt32(bs, 0)

let stringToBytes (s:string) = Encoding.ASCII.GetBytes s
let bytesToString = Encoding.ASCII.GetString

let arraySkip count arr =
    let l = Array.length arr
    Array.sub arr count (l-count)