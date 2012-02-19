namespace the_end

type ServerQueryDetails = { desc : string; numPlayers : string; numSlots : string }

type Server (name) =
    class

        //let mutable connections = []

        let comms = ServerComms(name)

        member this.Comms = comms
        member this.Details : ServerQueryDetails = { desc = "?"; numPlayers = "12"; numSlots = "345" }

    end
