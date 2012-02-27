#nowarn "25"
namespace the_end

open System
open System.IO
open Microsoft.FSharp.Core.Operators
open System.Net.Sockets

open System.Text

exception BadCommException

type McSlot = 
    | Empty
    | Full of sbyte * int16 * (byte[] option)

type McType =
    | TId
    | TUByte
    | TSByte
    | TShort
    | TInt
    | TLong
    | TSingle
    | TDouble
    | TString
    | TBool
    | TRawBytes of int
    | TSlot

type McData =
    | Id of int
    | UByte of byte
    | SByte of sbyte
    | Short of int16
    | Int of int
    | Long of int64
    | Single of float32
    | Double of float
    | String of string
    | Bool of bool
    | RawBytes of byte[]
    | Slot of McSlot

type McSocket (tc : TcpClient) =
    
    class

        // write buffering stream. minecraft is fussy about how we send packets :(
        let ws = new BufferedStream(tc.GetStream(), 4096)

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

        //let writeBEChars (strong : string) = tc.GetStream().AsyncWrite (Encoding.BigEndianUnicode.GetBytes(strong))
        let writeBEChars (strong : string) = ws.AsyncWrite (Encoding.BigEndianUnicode.GetBytes(strong))

        // reads the specified number of bytes, converting from network order to little-endian if system is little-endian
        let read (n : int) = async {
            let! (bytes : byte[]) = tc.GetStream().AsyncRead(n)
            return revBytes bytes
        }

        
        //let wr bs = tc.GetStream().AsyncWrite (revBytes bs)
        let wr bs = ws.AsyncWrite (revBytes bs)


        let grah (a: int, b : int) = a + b

        let readc (n : int) (bcfun : (byte[] * int) -> 'a) = async {
            let! bs = read n
            return (bcfun (bs, 0))
        }

        let rstring () = async {
            let! strlen = readc 2 BitConverter.ToInt16 // read short

            if strlen > (int16 0) then
                printfn "reading string of %i characters" strlen
                let! str = readBEChars(int strlen)
                return (String str)
            else if strlen = (int16 0) then
                printfn "not reading string of 0 chars; passing back ''"
                return (String "")
            else
                printfn "er, got negative string length (%i)... closing conn." strlen
                tc.Close ()  // TODO: kick properly, instead?
                return (String "")
        }

        let itemBlockIdsWithExtraSlotData =
            let thoseIds = [0x103;0x105;0x15a;0x167;0x10c;0x10d;0x10e;0x10f;0x122;0x110;0x111;0x112;0x113;0x123;0x10b;0x100;0x101;0x102;0x124;0x114;0x115;0x116;0x117;0x125;0x11b;0x11c;0x11d;0x11e;0x126;0x12a;0x12b;0x12c;0x12d;0x12e;0x12f;0x130;0x131;0x132;0x133;0x134;0x135;0x136;0x137;0x138;0x139;0x13a;0x13b;0x13c;0x13d]
            Array.init 32768 (fun x -> List.exists (fun y -> y = x) thoseIds)

        let rec readMc (mt : McType) = 
            async {
                match mt with
                    | TId         -> let! id = readc 1 (fun (bs, _) -> bs.[0])
                                     return (Id (int id))
                    | TUByte      -> let! ub = readc 1 (fun (bs, _) -> bs.[0])
                                     return (UByte ub)
                    | TSByte      -> let! sb = readc 1 (fun (bs, _) -> (sbyte bs.[0]))
                                     return (SByte sb)
                    | TShort      -> let! sh = readc 2 BitConverter.ToInt16
                                     return (Short sh)
                    | TInt        -> let! i = readc 4 BitConverter.ToInt32
                                     return (Int i)
                    | TLong       -> let! l = readc 8 BitConverter.ToInt64
                                     return (Long l)
                    | TSingle     -> let! s = readc 4 BitConverter.ToSingle
                                     return (Single s)
                    | TDouble     -> let! d = readc 8 BitConverter.ToDouble
                                     return (Double d)
                    | TString     -> return! rstring()
                    | TBool       -> let! b = readc 1 (fun (bs, _) -> bs.[0]) // read unsigned byte
                                     return (Bool (if b = (byte 0) then false else true)) // TODO: check for other values?
                    | TRawBytes n -> let! bs = tc.GetStream().AsyncRead(n)
                                     return (RawBytes bs)
                    | TSlot       -> return! readSlot() 
            }
        and readSlot () = 
            async {
                let! (Short itemBlockId) = readMc(TShort)

                if itemBlockId = (int16 -1) then
                    return Slot (Empty)
                else
                    let! (SByte count) = readMc(TSByte)
                    let! (Short damage) = readMc(TShort)
                    // aaaargh... `every item that has a 'damage bar' in-game is considered enchantable
                    //             by the protocol, though the notchian server/client do not support enchantment
                    //             of some items.'
                    if itemBlockIdsWithExtraSlotData.[(int itemBlockId)] then
                        // gotta read extra data
                        let! (Short arrLen) = readMc(TShort)
                        let! (RawBytes arr) = readMc(TRawBytes (int arrLen))
                        return Slot (Full (count, damage, Some(arr)))
                    else
                        // nop!
                        return Slot (Full (count, damage, None))

            }


        let writeMc (md : McData) =
            match md with
                | RawBytes bs -> ws.AsyncWrite bs
                | Id id       -> wr [|(byte id)|]
                | UByte ub    -> wr [|ub|]
                | SByte sb    -> wr [|(byte sb)|]
                | Short sh    -> wr (BitConverter.GetBytes(sh))
                | Int i       -> wr (BitConverter.GetBytes(i))
                | Long l      -> wr (BitConverter.GetBytes(l))
                | Single s    -> wr (BitConverter.GetBytes(s))
                | Double d    -> wr (BitConverter.GetBytes(d))
                | Bool b      -> wr [|byte (if b then 1 else 0)|]
                | String str  -> async {
                                    do! wr (BitConverter.GetBytes(int16 (str.Length))) // write short
                                    do! writeBEChars (str)
                                 }
                // TODO: slot support!


        let rec wrinbody (md : McData, tail : McData list) = 
            async {
                do! (writeMc md)
                do! (wrinner tail)
            }
        and wrinner (mds : McData list) = 
            async {
                match mds with
                    | [] -> return ()
                    | md::tail -> do! wrinbody(md, tail)
            }

        member this.Read (mts : McType list) = async {
            match mts with
                | [] -> return []
                | mt::tail -> let! x = (readMc mt)
                              let! xs = (this.Read tail)
                              return x::xs
        }

        member this.ReadId() = async {
            let! (Id id) = readMc(TId)
            return id
        }


        member this.Write (mds : McData list) = async {
            do! wrinner(mds) // write to buffer
            ws.Flush() // flush buffer (so actually write to stream)
        }

        member this.Kick (msg : string) = this.Write [UByte (byte 0xff); String msg]

        member this.Available = tc.Available
        member this.Connected = tc.Connected
        member this.Close () = tc.Close

        override this.Finalize() = ws.Dispose()

    end
