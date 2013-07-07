module Crypto

open System
open System.Security.Cryptography

let rng = new RNGCryptoServiceProvider()

let randomBytes nbytes =
    let data = Array.zeroCreate<byte> nbytes 
    rng.GetBytes data
    data

