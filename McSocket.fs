namespace the_end

open System
open Microsoft.FSharp.Core.Operators
open System.Net.Sockets

open System.Text

exception BadCommException

type McType =
    | SByte
    | SShort
    | SInt
    | SLong
    | Float
    | Double
    | String
    | Bool

type McSocket (tc : TcpClient) =
    
    class

        // reverses a given byte array, and returns this new array
        let revBytes (bytes : byte[]) =
            if BitConverter.IsLittleEndian then
                Array.init bytes.Length (fun i -> bytes.[bytes.Length - i - 1])
            else
                bytes

        let readBEChars (n : int) = async {
            let! (bytes : byte[]) = tc.GetStream().AsyncRead(n * 2)
            return Encoding.BigEndianUnicode.GetString bytes
        }

        let writeBEChars (strong : string) = tc.GetStream().AsyncWrite (Encoding.BigEndianUnicode.GetBytes(strong))

        // reads the specified number of bytes, converting from network order to little-endian if system is little-endian
        let read (n : int) = async {
            let! (bytes : byte[]) = tc.GetStream().AsyncRead(n)
            return revBytes bytes
        }
        
        let wr bs = tc.GetStream().AsyncWrite (revBytes bs)

        let grah (a: int, b : int) = a + b

        let readc (n : int) (bcfun : (byte[] * int) -> 'a) = async {
            let! bs = read n
            return (bcfun (bs, 0))
        }
    
        

        member this.rub () = readc 1 (fun (bs, _) -> bs.[0])
        member this.rsb () = readc 1 (fun (bs, _) -> (sbyte bs.[0]))
        member this.rss () = readc 2 BitConverter.ToInt16
        member this.rsi () = readc 4 BitConverter.ToInt32
        member this.rsl () = readc 8 BitConverter.ToInt64
        member this.rsingle () = readc 4 BitConverter.ToSingle
        member this.rdouble () = readc 8 BitConverter.ToDouble

        // reads an unsigned byte, like this.rub, but casts it to an int afterwards.
        // casting between bytes and ints is annoying. (have to b/c e.g. 0x01 is an int by default, not a byte!)
        member this.rid () = async {
            let! idb = this.rub()
            return (int idb)
        }
        
        member this.rstring () = async {
            let! strlen = this.rss()

            if strlen > (int16 0) then
                printfn "reading string of %i characters" strlen
                return! readBEChars(int strlen)
            else if strlen = (int16 0) then
                printfn "not reading string of 0 chars; passing back ''"
                return ""
            else
                printfn "er, got negative string length (%i)... closing conn." strlen
                tc.Close ()
                return ""
        }

        member this.wub (b : byte) = wr [|b|]
        member this.wsb (b : sbyte) = wr [|(byte b)|]
        member this.wss (s : int16) = wr (BitConverter.GetBytes(s))
        member this.wsi (i : int32) = wr (BitConverter.GetBytes(i))
        member this.wsl (l : int64) = wr (BitConverter.GetBytes(l))
        member this.wsingle (si : float32) = wr (BitConverter.GetBytes(si))
        member this.wdouble (double : float) = wr (BitConverter.GetBytes(double))

        member this.wid (id : int) = wr [|(byte id)|]

        member this.wstring (strong : string) = async {
            do! this.wss (int16 strong.Length)
            do! writeBEChars (strong)
        }


        member this.Kick (msg : string) = async {
            do! this.wub (byte 0xFF)
            do! this.wstring msg
        }




        member this.Available = tc.Available
        member this.Connected = tc.Connected
        member this.Close () = tc.Close

    end