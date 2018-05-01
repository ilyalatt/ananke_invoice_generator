module Tmp

open FSharpx
open FSharpx
open System
open System.IO

[<AbstractClass>]
type Disposable() =
    abstract member cleanup: bool -> unit

    interface IDisposable with
        member this.Dispose() =
            do this.cleanup(true)
            GC.SuppressFinalize(this)

    override this.Finalize() =
        do this.cleanup(false)

let delFile path =
    if File.Exists path then
        do File.SetAttributes(path, FileAttributes.Normal)
        do File.Delete(path)


type TmpFile(path) =
    inherit Disposable()
    member this.Path = path

    override this.cleanup _ =
        do delFile this.Path

type TmpDir(path) =
    inherit Disposable()
    member this.Path = path

    override this.cleanup _ =
        do IO.Directory.Delete(this.Path, true)

let getTmpFile _ =
    new TmpFile(Path.GetTempFileName())

let getTmpDir _ =
    let tmpPath = Path.GetTempFileName()
    do delFile tmpPath
    let dirName = Path.GetFileNameWithoutExtension(tmpPath)
    let dirPath = Path.Combine(Path.GetDirectoryName(tmpPath), dirName)
    do Directory.CreateDirectory(dirPath) |> ignore
    new TmpFile(dirPath)

let writeToTmpFile s =
    let tmpFile = getTmpFile()
    do IO.writeStringToFile false tmpFile.Path s
    tmpFile

let rec copyDir (fromDir, toDir) =
    do Directory.CreateDirectory(toDir) |> ignore

    let updateSnd (a, b) = (a, IO.combinePaths b <| Path.GetFileName(a))
    let updateTos froms = froms |> Seq.map (fun x -> updateSnd(x, toDir))

    do fromDir |> Directory.GetDirectories |> updateTos |> Seq.iter copyDir
    do fromDir |> Directory.GetFiles |> updateTos |> Seq.iter File.Copy

let cloneInTmpDir dirPath =
    let tmpDir = getTmpDir()
    do copyDir(dirPath, tmpDir.Path)
    tmpDir
