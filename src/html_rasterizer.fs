module HtmlRasterizer

open FSharpx
open System.Runtime.InteropServices
open System.Diagnostics
open System.IO

// based on the https://github.com/TheSalarKhan/PhantomJs.NetCore

let phantomJsDir = "phantom_js"

type Platform =
| Windows
| Linux
| MacOS

let getPlatform _ =
    if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then Windows
    else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then Linux
    else if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then MacOS
    else failwithf "the OS is not supported"

let getPhantomJsFileNamePrefix =
    function
    | Windows -> "windows"
    | Linux -> "linux"
    | MacOS -> "macos"

let getPhantomJsFileName platform =
    getPhantomJsFileNamePrefix platform + "_phantomjs.exe"


let getPhantomJsRasterizationScript _ =
    let assembly = System.Reflection.Assembly.GetEntryAssembly()
    use resourceStream = assembly.GetManifestResourceStream("ananke_invoice_generator.phantom_js_rasterize.js");
    use reader = new StreamReader(resourceStream, System.Text.Encoding.UTF8)
    reader.ReadToEndAsync().Result;


let executePhantomJs phantomJsPath phantomJsScriptPath inputPath outputPath =
    let quote s = sprintf "\"%s\"" s
    let args =
        [ phantomJsScriptPath; inputPath; outputPath ]
        |> Seq.map quote |> String.concat " "
    let startInfo =
        new ProcessStartInfo(
            phantomJsPath, args,
            UseShellExecute = false
        )

    let proc = Process.Start(startInfo)
    do proc.WaitForExit()
    ()

let htmlToPdf inputPath outputPath =
    let phantomJsFileName = getPlatform() |> getPhantomJsFileName
    let phantomJsFilePath = IO.combinePaths phantomJsDir phantomJsFileName
    let phantomJsScript = getPhantomJsRasterizationScript() |> Tmp.writeToTmpFile

    let outputPathDir = Path.GetDirectoryName(outputPath)
    if not <| Directory.Exists(outputPathDir) then
        do Directory.CreateDirectory(outputPathDir) |> ignore

    do executePhantomJs phantomJsFilePath phantomJsScript.Path inputPath outputPath
