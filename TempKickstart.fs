namespace the_end

open System.Net.Sockets
open System.Threading

open Ionic.Zlib

// kickstarts a minecraft session.
type TempKickstart (tc : TcpClient, m : McSocket) =
    class

        // load=true: load chunk, load=false: unload chunk
        let write_prechunk (x,z,load) = async {
            printfn "prechunk (x:%d, z:%d) load: %b" x z load
            do! m.Write [Id 0x32; Int x; Int z; Bool true]
            //do! m.wid 0x32
            //do! m.wsi x
            //do! m.wsi z
            //do! m.wub (byte 1)
            //do! m.wend()
        }

        let write_mapchunk (x,z,primarybitmap,compregion : byte array) = async {
            printfn "mapchunk (x:%d, z:%d) pb: %d  cr len: %d" x z primarybitmap compregion.Length
            do! m.Write [Id 0x33; Int x; Int z; Bool false; Short (int16 primarybitmap);
                            Short (int16 0); Int compregion.Length; Int 0; RawBytes compregion]
            //do! m.wid 0x33
            //do! m.wsi x // chunk x coord (*16 for block start coord)
            //do! m.wsi z // chunk z coord (*16 for block start coord)
            //do! m.wbool false // ground-up contiguous: always false
            //do! m.wss (int16 primarybitmap)
            //do! m.wss (int16 0) // add bitmap
            //do! m.wsi compregion.Length // compressed region data size
            //do! m.wsi 0 // ?
            //do! m.writeRaw compregion // compressed region data
            //do! m.wend()
        }

        let gen_test_region () =
            let b : byte = (byte 87)
            let metadatalight : byte = (byte 15)
            let skylight = (byte 15)
            let numblocks = 16*256*16
            let biome = (byte 0)
            [| for i in 1..(numblocks/4) -> b // quarter the region is netherrack
               for i in 1..(3*numblocks/4) -> (byte 0) // 3/4 the region is air
               for i in 1..numblocks -> metadatalight // block metadata nibble, block light nibble
               for i in 1..(numblocks/2) -> skylight // sky light array, 1 nibble/block
               for i in 1..(16*16) -> biome // biome; 1 byte per XZ column
               |]
            

        let setup () = async {

            Thread.Sleep(100)

            // send initial chunks
            let dat_chunk_list = { -1..1 }

            // -- prechunk:
            for z in dat_chunk_list do // z coord
                for x in dat_chunk_list do // x coord
                    do! write_prechunk(x,z,true)

            // -- map data:
            let r = gen_test_region()
            let comp_r = ZlibStream.CompressBuffer(r)
            for z in dat_chunk_list do
                for x in dat_chunk_list do
                    do! write_mapchunk(x,z,65535,comp_r)

            printfn "set spawn position.."
            do! m.Write [Id 0x06; Int 0; Int 70; Int 0]
            //do! m.wid 0x06
            //do! m.wsi 0
            //do! m.wsi 70
            //do! m.wsi 0
            //do! m.wend()

            printfn "0x0d..."
            do! m.Write [Id 0x0d; Double 0.0; Double 70.0; Double 70.0; Double 0.0;
                            Single 0.0f; Single 0.0f; Bool true]
            //do! m.wid 0x0d
            //do! m.wdouble 0.0 // x
            //do! m.wdouble 70.0 // stance
            //do! m.wdouble 70.0 // y
            //do! m.wdouble 0.0 // z
            //do! m.wsingle 0.0f // yaw
            //do! m.wsingle 0.0f // pitch
            //do! m.wbool true // on ground?
            //do! m.wend()

            printfn "done."
        }

        member this.do_it () = async {

            do! setup()

        }

    end
