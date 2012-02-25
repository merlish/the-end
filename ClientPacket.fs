namespace the_end

// http://wiki.vg/Protocol

type DiggingStatus = 
    | StartedDigging
    | FinishedDigging
    | DropItem // grr. weird ass special case
    | ShootArrowFinishEating // -.-

type ClientPacket =
    | KeepAlive of KeepAliveID // 0x00
    | LoginRequest of ProtocolVersion * string // 0x01
    | Handshake of string // 0x02
    | ChatMessage of UnsanitizedChatMessage // 0x03
    | UseEntity of EntityID * EntityID * bool // 0x07
    | Respawn of int * byte * byte * int16 * string // 0x09
    | Player of bool // 0x0a
    | PlayerPosition of float * float * float * float * bool // 0x0b
    | PlayerLook of float32 * float32 * bool // 0x0c
    | PlayerPositionAndLook of float * float * float * float * float32 * float32 * bool // 0x0d
    | PlayerDigging of DiggingStatus * BlockCoord * byte // 0x0e
    | PlayerBlockPlacement of BlockCoord * byte // 0x0f
    | HoldingChange of int16 // 0x10
    | UseBed of EntityID * byte * BlockCoord // 0x11
    | Animation of EntityID * byte // 0x12
    | EntityAction of EntityID * byte // 0x13
    | CloseWindow of WindowID // 0x65
    | WindowClick of WindowID * SlotID * byte * TransactionID * bool * Slot // 0x66
    | Transaction of WindowID * TransactionID * bool // 0x6a
    | EnchantItem of WindowID * byte // 0x6c
    | UpdateSign of BlockCoord * string * string * string * string // 0x82
    | PluginMessage of string * int16 * byte[] // 0xfa
    | ServerListPing // 0xfe
    | Disconnect of string // 0xff
    | Invalid of int // unknown packet id

    // | IncrementStatistic of int * byte (?)

type ClientPacketReader =

    let readKeepAlive(m : McSocket) = async {
        let! kid = m.rsi()
        return KeepAlive kid
    }

    let readLoginRequest (m : McSocket) 

    let readClientPacket(m : McSocket) = async {
        
        let! id = m.rid()

        match id with
            | 0x00 -> return! readKeepAlive m
            | 0x01 -> return! readLoginRequest m
            | 0x02 -> return! readHandshake m
            | 0x03 -> return! readChatMessage m
            | 0x07 -> return! readUseEntity m
            | 0x09 -> return! readRespawn m
            | 0x0a -> return! readPlayer m
            | 0x0b -> return! readPlayerPosition m
            | 0x0c -> return! readPlayerLook m
            | 0x0d -> return! readPlayerPositionAndLook m
            | 0x0e -> return! readPlayerDigging m
            | 0x0f -> return! readPlayerBlockPlacement m
            | 0x10 -> return! readHoldingChange m
            | 0x11 -> return! readUseBed m
            | 0x12 -> return! readAnimation m
            | 0x13 -> return! readEntityAction m
            | 0x65 -> return! readCloseWindow m
            | 0x66 -> return! readWindowClick m
            | 0x6a -> return! readTransaction m
            | 0x6c -> return! readEnchantItem m
            | 0x82 -> return! readUpdateSign m
            | 0xfa -> return! readPluginMessage m
            | 0xfe -> return! readServerListPing m
            | 0xff -> return! readDisconnect m
            | _    -> return Invalid id
    }

    
