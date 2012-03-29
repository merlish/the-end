#nowarn "25"
namespace the_end

// http://wiki.vg/Protocol

// It is the responsibility of the caller to ensure that data is valid.
// This thang just tries to read the packets.

type ClientPacket =
    | KeepAlive of KeepAliveID // 0x00
    | LoginRequest of ProtocolVersion * string // 0x01
    | Handshake of string // 0x02
    | ChatMessage of UnsanitizedChatMessage // 0x03
    | UseEntity of EntityID * EntityID * bool // 0x07
    | Respawn of int * sbyte * sbyte * int16 * string // 0x09
    | Player of bool // 0x0a
    | PlayerPosition of float * float * float * float * bool // 0x0b
    | PlayerLook of float32 * float32 * bool // 0x0c
    | PlayerPositionAndLook of float * float * float * float * float32 * float32 * bool // 0x0d
    | PlayerDigging of sbyte * BlockCoord * sbyte // 0x0e
    | PlayerBlockPlacement of BlockCoord * sbyte * McSlot // 0x0f
    | HoldingChange of SlotID // 0x10
    | UseBed of EntityID * sbyte * BlockCoord // 0x11
    | Animation of EntityID * sbyte // 0x12
    | EntityAction of EntityID * sbyte // 0x13
    | CloseWindow of WindowID // 0x65
    | WindowClick of WindowID * SlotID * sbyte * TransactionID * bool * McSlot // 0x66
    | Transaction of WindowID * TransactionID * bool // 0x6a
    | EnchantItem of WindowID * sbyte // 0x6c
    | UpdateSign of BlockCoord * string * string * string * string // 0x82
    | PluginMessage of string * byte[] // 0xfa
    | ServerListPing // 0xfe
    | Disconnect of string // 0xff
    | Invalid of int // unknown packet id

    // | IncrementStatistic of int * byte (?)

type ClientPacketReader (m : McSocket) =
    class

        let readKeepAlive = async {
            let! [Int kid] = (m.Read [TInt])
            return KeepAlive kid
        }

        let readLoginRequest = async {
            let! [Int protVer; String user; String _; Int _; Int _; SByte _; SByte _; UByte _]
                = (m.Read [TInt; TString; TString; TInt; TInt; TSByte; TSByte; TUByte])
            return LoginRequest (protVer, user)
        }

        let readHandshake = async {
            let! [String uname] = (m.Read [TString])
            return Handshake uname
        }

        let readChatMessage = async {
            let! [String msg] = (m.Read [TString])
            return ChatMessage msg
        }

        let readUseEntity = async {
            let! [Int user; Int target; Bool leftClick] = (m.Read [TInt; TInt; TBool])
            return UseEntity (user, target, leftClick)
        }

        let readRespawn = async {
            let! [Int dimension; SByte difficulty; SByte creativeMode; Short worldHeight; String levelType]
                = (m.Read [TInt; TSByte; TSByte; TShort; TString])
            return Respawn (dimension, difficulty, creativeMode, worldHeight, levelType)
        }

        let readPlayer = async {
            let! [Bool onGround] = (m.Read [TBool])
            return Player (onGround)
        }

        let readPlayerPosition = async {
            let! [Double x; Double y; Double stance; Double z; Bool onGround] = (m.Read [TDouble; TDouble; TDouble; TDouble; TBool])
            return PlayerPosition (x, y, stance, z, onGround)
        }

        let readPlayerLook = async {
            let! [Single yaw; Single pitch; Bool onGround] = m.Read [TSingle; TSingle; TBool]
            return PlayerLook (yaw, pitch, onGround)
        }
        
        let readPlayerPositionAndLook = async {
            let! [Double x; Double y; Double stance; Double z; Single yaw; Single pitch; Bool onGround]
                = m.Read [TDouble; TDouble; TDouble; TDouble; TSingle; TSingle; TBool]
            return PlayerPositionAndLook (x, y, stance, z, yaw, pitch, onGround)
        }

        let readPlayerDigging = async {
            let! [SByte status; Int x; SByte y; Int z; SByte face] = m.Read [TSByte; TInt; TSByte; TInt; TSByte]
            return PlayerDigging (status, (x, y, z), face)
        }

        let readPlayerBlockPlacement = async {
            let! [Int x; SByte y; Int z; SByte direction; Slot slot] = m.Read [TInt; TSByte; TInt; TSByte; TSlot]
            return PlayerBlockPlacement ((x, y, z), direction, slot)
        }

        let readHoldingChange = async {
            let! [Short slotId] = m.Read [TShort]
            return HoldingChange (slotId)
        }

        let readUseBed = async {
            let! [Int eid; SByte inBed; Int x; SByte y; Int z] = m.Read [TInt; TSByte; TInt; TSByte; TInt]
            return UseBed (eid, inBed, (x, y, z))
        }

        let readAnimation = async {
            let! [Int eid; SByte animation] = m.Read [TInt; TSByte]
            return Animation (eid, animation)
        }

        let readEntityAction = async {
            let! [Int eid; SByte aid] = m.Read [TInt; TSByte]
            return EntityAction (eid, aid)
        }

        let readCloseWindow = async {
            let! [SByte wid] = m.Read [TSByte]
            return CloseWindow (wid)
        }

        let readWindowClick = async {
            let! [SByte wid; Short slotId; SByte rightClick; Short trId; Bool shift; Slot clickedItem]
                = m.Read [TSByte; TShort; TSByte; TShort; TBool; TSlot]
            return WindowClick (wid, slotId, rightClick, trId, shift, clickedItem)
        }

        let readTransaction = async {
            let! [SByte wid; Short trId; Bool accepted] = m.Read [TSByte; TShort; TBool]
            return Transaction (wid, trId, accepted)
        }

        let readEnchantItem = async {
            let! [SByte wid; SByte enchantment] = m.Read [TSByte; TSByte]
            return EnchantItem (wid, enchantment)
        }

        let readUpdateSign = async {
            let! [Int x; Short y; Int z; String line1; String line2; String line3; String line4]
                = m.Read [TInt; TShort; TInt; TString; TString; TString; TString]
            // bizarre: only time a Y block coord is given as a short O.o
            return UpdateSign ((x, (sbyte y), z), line1, line2, line3, line4)
        }

        let readPluginMessage = async {
            let! [String channel; Short arrLen] = m.Read [TString; TShort]
            let! [RawBytes arr] = m.Read [TRawBytes (int arrLen)]
            // TODO: what if arrLen is e.g. negative?
            return PluginMessage (channel, arr)
        }

        let readServerListPing = async {
            return ServerListPing
        }

        let readDisconnect = async {
            let! [String reason] = m.Read [TString]
            return Disconnect (reason)
        }
            

        let readClientPacket() = async {
            
            let! id = m.ReadId()

            match id with
                | 0x00 -> return! readKeepAlive 
                | 0x01 -> return! readLoginRequest 
                | 0x02 -> return! readHandshake 
                | 0x03 -> return! readChatMessage 
                | 0x07 -> return! readUseEntity 
                | 0x09 -> return! readRespawn 
                | 0x0a -> return! readPlayer 
                | 0x0b -> return! readPlayerPosition 
                | 0x0c -> return! readPlayerLook 
                | 0x0d -> return! readPlayerPositionAndLook 
                | 0x0e -> return! readPlayerDigging 
                | 0x0f -> return! readPlayerBlockPlacement 
                | 0x10 -> return! readHoldingChange 
                | 0x11 -> return! readUseBed 
                | 0x12 -> return! readAnimation 
                | 0x13 -> return! readEntityAction 
                | 0x65 -> return! readCloseWindow 
                | 0x66 -> return! readWindowClick 
                | 0x6a -> return! readTransaction 
                | 0x6c -> return! readEnchantItem 
                | 0x82 -> return! readUpdateSign 
                | 0xfa -> return! readPluginMessage 
                | 0xfe -> return! readServerListPing 
                | 0xff -> return! readDisconnect 
                | _    -> return Invalid id
        }

        member this.Read() = readClientPacket()

    end

        
