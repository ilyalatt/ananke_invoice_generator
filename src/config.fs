module Config

open FSharp.Interop.Dynamic
open FSharpx

let configName1 = "config.local.yaml"
let configName2 = "config.yaml"

let checkFile path =
    if System.IO.File.Exists(path) then Some path else None

let readConfig _ =
    let configName =
        checkFile configName1
        |> Option.orElseWith (fun _ -> checkFile configName2)
        |> Option.get
    let deserializer = YamlDotNet.Serialization.Deserializer()
    let config = deserializer.Deserialize<obj>(IO.readFileAsString configName)
    config

// TODO: remove FSharp.Interop.Dynamic dependency
let ($) objOpt (name: string) =
    try
        objOpt |> Option.bind (fun obj -> obj?get_Item(name) |> Option.ofObj)
    with
        | :? System.Collections.Generic.KeyNotFoundException -> None
        | :? System.NullReferenceException -> None
let cfgKeys cfgObj = cfgObj?Keys |>  Seq.map string |> List.ofSeq
let cfgSet name value cfgObj = cfgObj?set_Item(name, value)
