namespace the_end

type ServerQueryDetails = { desc : string; numPlayers : string; numSlots : string }

type Server =
    class

        let mutable connections 

        member this.Details : ServerQueryDetails = { desc = "?"; numPlayers = "12"; numSlots = "345" }

    end
