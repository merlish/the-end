namespace the_end

open System
open System.Net
open System.Net.Sockets
open System.Threading

type ServerSelector (listener : TcpListener) =

    class

        let mutable (servers : Server list) = []

        let guessQueryDetails (clientEP : EndPoint) =
            // minecraft only sends vhost-friendly 'who am i connecting to' details on actual connection,
            //  not on query, so we can't be sure when queried whose details we should return.

            // TODO: ask mc dev to change protocol // build smart guess cache
            
            // for now, let's just handle the simple cases.
            // if there are no servers, say so. if 1, do that. else, say we're a multi-serv.
            let ss = servers
            if ss.Length = 0 then
                { desc = "(server not loaded)"; numPlayers = "0"; numSlots = "0" }
            else if ss.Length = 1 then
                servers.Head.Details
            else 
                { desc = "multihome server. click connect!"; numPlayers = "?"; numSlots = "?" }

        

        let clifun (cli : TcpClient) _ = async {
            // don't wait more than 10 seconds between message parts
            cli.ReceiveTimeout <- 10000

            let mcs = McSocket(cli);
            //cli.Client.LocalEndPoint.
            // await 0x02 handshake packet/0xfe server query packet from client
            //printfn "await 0x02/0xfe"
            let! x = mcs.rid()
            
            //printfn "got something alright"

            if x = 0x02 then
                let! userAndHost = mcs.rstring ()

            else if x = 0xFE then
                let qd = guessQueryDetails cli.Client.RemoteEndPoint
                return! mcs.Kick(qd.desc + "§" + qd.numPlayers + "§" + qd.numSlots)
            else 
                return! mcs.Kick("bad handshake: expected 0x02 or 0xFE")


            // restore default infinite receive timeout
            cli.ReceiveTimeout <- -1
        }
        

        let holder = TcpClientHolder(clifun, "Server Selector TcpClientHolder")

        let rec listenfun () =
            holder.post (listener.AcceptTcpClient())
            listenfun() // loop

        do
            let thr = Thread(listenfun)
            thr.Start();



        member this.AddServer (s : Server) =
            servers <- (s :: servers)

    end