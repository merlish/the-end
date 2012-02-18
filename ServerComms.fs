namespace the_end

open System
open System.Net.Sockets

// handles player<->server communications for one server
type ServerComms (serverName : string) =
    class
        
        let serverProtocolVersion = 27

        let clifun (cli : TcpClient) = async {
            
            // ok.  we've just got a new TcpClient, so we know it's sent a correct
            //  handshake (0x02) packet client->server.

            let m = McSocket(cli)
            let rep = cli.Client.RemoteEndPoint



            // secondly, respond to the handshake.
            // TODO: auth!
            do! m.wid 0x02
            do! m.wstring "-" // i.e. no auth

            // await 0x01 login request from client
            let! id = m.rid()

            if id <> 0x01 then
                printfn "stupid client %O sent packet %x not 0x01 during login. kicked" rep id
                return! m.Kick "expected 0x01 login request"


            // otherwise, let's read the data from the packet!
            let! protocolVersion = m.rsi()

            if protocolVersion <> serverProtocolVersion then
                printfn "stupid client %O is on version %i, not %i" rep protocolVersion serverProtocolVersion

            let! username = m.rstring()
            let! _ = m.rsl()
            let! _ = m.rstring()
            let! _ = m.rsi()
            let! _ = m.rsb()
            let! _ = m.rsb()
            let! _ = m.rub()
            let! _ = m.rub()

            

        }

        let holder = TcpClientHolder(clifun, "PlayerComms[" + serverName + "]")



        member this.Post = holder.post

    end
