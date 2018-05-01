module Report

open Config
open FSharpx
open Harvest
open System

type Project = {
    Name: string
    Hours: float
    Price: float
}

type Report = {
    Projects: Project list
    TotalHours: float
    TotalPrice: float
}

let getName timeEntry =
    let notes = timeEntry.Notes |> Option.map (fun x -> sprintf " (%s)" x) |> Option.defaultValue ""
    timeEntry.Project.Name + notes

let getNameCanonicalizer projectsCfg =
    projectsCfg |> Seq.map (Some >> fun project ->
        let projectName = project $ "name" |> Option.get |> string
        let aliasesOpt = project $ "aliases" |> Option.map (Seq.map string)
        let aliases = aliasesOpt |> Option.defaultValue Seq.empty |> Seq.append (Seq.singleton projectName)
        (projectName, aliases)
    )
    |> Seq.collect (fun (projectName, aliases) ->
        aliases |> Seq.map (fun alias -> (alias, projectName))
    )
    |> Map.ofSeq

let getNameToHourRateMap projectsCfg =
    projectsCfg |> Seq.map Some |> Seq.choose (fun project ->
        let projectName = project $ "name" |> Option.get |> string
        let hourRateOpt = project $ "hour_rate" |> Option.map Double.Parse
        hourRateOpt |> Option.map (fun hourRate -> (projectName, hourRate))
    )
    |> Map.ofSeq

let getReport reportCfg (harvestTimeEntries: TimeEntry list) =
    let defaultHourRate = reportCfg $ "default_hour_rate" |> Option.get |> Double.Parse
    let projectsCfg = reportCfg $ "projects" |> Option.defaultValue Seq.empty
    let nameCanonicalizer = projectsCfg |> getNameCanonicalizer
    let nameToHourRateMap = projectsCfg |> getNameToHourRateMap
    let getPrice = fun (name, hours) -> name |> flip Map.tryFind nameToHourRateMap |> Option.defaultValue defaultHourRate |> (*) hours
    let projects =
        harvestTimeEntries
        |> Seq.groupBy (fun  timeEntry ->
            let name = timeEntry |> getName
            name |> flip Map.tryFind nameCanonicalizer |> Option.defaultValue name
        )
        |> Seq.map (fun (name, timeEntries) ->
            let name = name |> flip Map.tryFind nameCanonicalizer |> Option.defaultValue name
            let hours = timeEntries |> Seq.sumBy (fun x -> x.Hours)
            { Name = name; Hours = hours; Price = getPrice (name, hours) }
        )
        |> List.ofSeq
    let totalHours = projects |> List.sumBy (fun x -> x.Hours)
    let totalPrice = projects |> List.sumBy (fun x -> x.Price)
    { Projects = projects; TotalHours = totalHours; TotalPrice = totalPrice }
