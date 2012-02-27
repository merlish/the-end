#nowarn "25"
namespace the_end

open System.Net.Sockets

// handles player<->server communications for one server
type ServerComms (serverName : string) =
    class
        
        let serverProtocolVersion = 28

        let clifun (cli : TcpClient) = async {
            
            // ok.  we've just got a new TcpClient, so we know it's sent a correct
            //  handshake (0x02) packet client->server.

            let m = McSocket(cli)
            let rep = cli.Client.RemoteEndPoint

            let fail reason =
                printfn "kicking client %O: %s" rep reason
                m.Kick reason

            // firstly, respond to the handshake.
            // TODO: auth!
            //do! m.wid 0x02
            //do! m.wstring "-" // i.e. no auth
            //do! m.wend()
            do! (m.Write [Id 0x02; String "-"])

            // await 0x01 login request from client
            let! id = m.ReadId()

            if id <> 0x01 then
                return! fail "expected 0x01 login request"

            // otherwise, let's read the data from the packet!
            let! [Int protocolVersion] = m.Read([TInt])

            if protocolVersion <> serverProtocolVersion then
                return! fail (sprintf "expected protocol version %i, not %i" serverProtocolVersion protocolVersion)

            let! [String username; _; _; _; _; _; _] = 
                m.Read([TString; TString; TInt; TSByte; TSByte; TUByte; TUByte])

            //let! username = m.rstring()
            //let! _ = m.rstring()
            //let! _ = m.rsi()
            //let! _ = m.rsb()
            //let! _ = m.rsb()
            //let! _ = m.rub()
            //let! _ = m.rub()

            if username.Length > 16 then
                return! fail "username too long"

            // ok, so far, so good.
            // let's accept the login request!
            // packet id, entity id of player, unused, level type, server mode, 
            //  dimension, difficulty, (deprecated), 
            //   max players
            do! (m.Write [Id 0x01; Int 1298; String ""; String "default"; Int 0; 
                            SByte (sbyte 0); SByte (sbyte 1); SByte (sbyte 0); 
                            UByte (byte 50)])

            //do! m.wid 0x01 // packet id
            // TODO: send actual Entity id
            //do! m.wsi 1298 // entity id of player
            //do! m.wstring "" // unused
            //do! m.wstring "default" // default or SUPERFLAT: level-type in server.properties
            //do! m.wsi 0 // server mode: 0, survival (1 for creative)
            //do! m.wsi 0 // ???
            //do! m.wub (byte 1)
            //do! m.wub (byte 0) // ???
            //do! m.wsb (sbyte 0) // dimension: -1: nether, 0: overworld, 1: the end (????)
            //do! m.wsb (sbyte 1) // difficulty: 0=peaceful, 1=easy, 2=normal, 3=hard (?????)
            //do! m.wsb (sbyte 0) // probably deprecated world height param (?????)
            //do! m.wub (byte 50) // max players; used by client to draw player list
            //do! m.wend()

            printfn "accepted login request.. doing tempkickstart\n"
            do! TempKickstart(cli,m).do_it()
        }

        let holder = TcpClientHolder(clifun, "PlayerComms[" + serverName + "]")



        member this.Post = holder.post

    end
