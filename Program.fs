namespace the_end

open System
open System.Net
open System.Net.Sockets

module Program =

    let asyncChild =
        async {
            printfn "oh hey"
            printfn "fbrrblt"
        }




    // interrogate the client for login data, so we can figure out which Server it should be
    //  being sent to.
    //let interrogate (client : TcpClient) =
    
    

    let listening port =
        let tl = TcpListener(IPAddress.Any, port)
        //let hold = TcpClientHolder(
        let sock = tl.AcceptTcpClient ()
    
        Console.WriteLine "listening"


    [<EntryPoint>]
    let main args =
        let listener = TcpListener(IPAddress.Any, 25535)
        listener.Start()
        let ss = ServerSelector(listener)
        ignore (Console.ReadLine ())
        0

