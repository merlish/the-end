namespace the_end_packetcode

module PacketReaderSrcSupport = 
    type argsOutput = string * string * string * string
    let sappend a b = sprintf "%s; %s" a b

    let mutable matchings = ""


    let read = "read"
    let name = "name"
    let id = "id"
    let is = "is"

    let ignore (structdefn, argunpack, readtype, recordstore) = (";", "_", readtype, "")

    //let tuple newArgName (sd, au, rt) (sd', au', rt') = (
    //let tuple (argname1, rt1, retval1) (argname2, rt2, retval2) = 
        //(sappend argname1 argname2, sappend rt1 rt2, sprintf "(%s, %s)" retval1 retval2)

    //let triple (an1, rt1, rv1) (an2, rt2, rv2) (an3, rt3, rv3) = 
        //(sappend (sappend an1 an2) an3, sappend (sappend rt1 rt2) rt3, sprintf "(%s, %s, %s)" rv1 rv2 rv3)

    //let immediately "read" (an, rt, rv) = (an, sprintf "%s]\n\t\t\tlet! [" rt, rv)

    let ftype fakeType realType argName = (sprintf "%s : %s" argName realType, sprintf "%s %s" fakeType argName, sprintf "T%s" fakeType, sprintf ".%s = %s" argName)
    //let SByte argName = (sprintf "%s : sbyte;" argName, sprintf "SByte %s" argName, "TSByte", ".%s = %s")
    let UByte = ftype "UByte" "byte"
    let SByte = ftype "SByte" "sbyte"
    let Short = ftype "Short" "int16"
    let Int = ftype "Int" "int"
    let Long = ftype "Long" "int64"
    let Single = ftype "Single" "float32"
    let Double = ftype "Double" "float"
    let String = ftype "String" "string"
    let Bool = ftype "Bool" "bool"
    let Slot = ftype "Slot" "McSlot"

    
    

    let unzip4 ins = (List.map (fun (a,_,_,_) -> a) ins, List.map (fun (_,b,_,_) -> b) ins, List.map (fun (_,_,c,_) -> c) ins, List.map (fun (_,_,_,d) -> d) ins)

    let RPacket "id" pid "name" pname "is" (arglist : argsOutput list) =
        let (allSd, allAu, allRt, allRs) = unzip4 arglist
        printfn "\t\ttype %s = { %s }\n" pname (List.reduce (sprintf "%s %s") allSd) // defn record for packet
        printfn "\t\tlet read%s = async {" pname // start reading code
        if arglist.Length > 0 then (
            let argunpacks = List.reduce (sprintf "%s; %s") allAu
            let readtypes = List.reduce (sprintf "%s; %s") allRt
            printfn "\t\t\tlet! [%s] = m.Read [%s]" argunpacks readtypes // async net read
            printfn "\t\t\treturn { %s }" (List.reduce (fun rs rs' -> if rs' = "" then rs else sprintf "%s; %s%s" rs pname rs') allRs)
        ) else
            printfn "\t\t\treturn ()"
        printfn "\t\t}\n" // end reading code.
        // and, record packet in the matchings list...
        matchings <- matchings + (sprintf "\t\t\t\t| %x -> let! r = read%s; return (box r)\n" pid pname)

    let AddCustomRPacket "id" pid "name" pname "is" code =
        printfn code
        matchings <- matchings + (sprintf "\t\t\t\t| %x -> let! r = read%s; return (box r)\n" pid pname)
        

    let Finish =
        printfn "\t\tlet readPacket = async {\n\n\t\t\tlet! id = m.ReadId()\n\n\t\t\tmatch id with\n%s\n\t\t}\n" matchings
        
        
        //printfn "let [" (Array.reduce(
    //and RPacket _ _ _ _ _ = failwith "RPacket call must be of form RPacket id (packet id) name (packet name) is (list of packet fields)."

