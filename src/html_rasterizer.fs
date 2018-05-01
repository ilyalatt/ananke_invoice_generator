module HtmlRasterizer

open FSharpx
open PuppeteerSharp
open System.IO

let htmlToPdf inputPath outputPath =
    printfn "download chromium"
    let revision = Downloader.DefaultRevision
    do Downloader.CreateDefault().DownloadRevisionAsync(revision).Wait()

    printfn "generate pdf"
    let browser = Puppeteer.LaunchAsync(new LaunchOptions(Headless = true), revision).Result;
    let page = browser.NewPageAsync().Result;
    do page.GoToAsync("file:///" + inputPath).Wait();

    let outputPathDir = Path.GetDirectoryName(outputPath)
    if not <| Directory.Exists(outputPathDir) then
        do Directory.CreateDirectory(outputPathDir) |> ignore
    do page.PdfAsync(outputPath).Wait();
