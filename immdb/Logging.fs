module Logging

open System
open System.IO
open Util

type LogLevel =
    | Network = 0

type Logger =
    static member private lockObj = new Object()
    static member private logFile = ref <| Path.Combine(Environment.CurrentDirectory, sprintf "immdb.%s.log" <| DateTime.UtcNow.Ticks.ToString())
    static member private logStream = ref <| new FileStream(Logger.LogFile, FileMode.Append)
    static member LogFile
        with get () = !Logger.logFile
        and  set n =
            Logger.logFile := n
            (!Logger.logStream).Dispose()
            Logger.logStream := new FileStream(Logger.LogFile, FileMode.Append)

    static member private logLevel = ref LogLevel.Network
    static member LogLevel
        with get () = !Logger.logLevel
        and  set l  = Logger.logLevel := l

    static member private showOnStdout = ref true
    static member ShowOnStdout
        with get () = !Logger.showOnStdout
        and  set b  = Logger.showOnStdout := b

    static member Log (level:LogLevel) str =
        if int level >= int Logger.LogLevel then
            let data = stringToBytes <| str + "\n"
            
            //lock Logger.lockObj <| fun () ->
            //    Logger.logStream.Value.Write(data, 0, data.Length)

            if Logger.ShowOnStdout then
                Console.WriteLine str

    static member LogAsync (level:LogLevel) str = async {
        if int level >= int Logger.LogLevel then
            let data = stringToBytes <| str + "\n"
            
            //do! lock Logger.lockObj <| fun () ->
            //    Logger.logStream.Value.AsyncWrite data

            if Logger.ShowOnStdout then
                Console.WriteLine str
    }