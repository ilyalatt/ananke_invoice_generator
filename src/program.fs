open Config
open Harvest
open Report
open InvoicePdfGenerator
open System
open FluentDateTime

let invoiceDateRange =
    let month = DateTime.Now.PreviousMonth()
    let fromDate = month.FirstDayOfMonth().BeginningOfDay()
    let toDate = month.LastDayOfMonth().EndOfDay()
    (fromDate, toDate)

let invoiceDate = invoiceDateRange |> snd |> (fun x -> x.NextDay().BeginningOfDay())
let invDateStr (format: string) = invoiceDate.ToString(format)
let invoiceFileName = invDateStr "yyyy.MM.dd"
let invoiceFilePath = sprintf "invoices/%s.pdf" invoiceFileName
let isInvoiceExists = IO.File.Exists(invoiceFilePath)

let getHarvestTimeEntries cfg =
    let harvestCfg = cfg $ "harvest"
    let auth = { AccountId = harvestCfg $ "account_id" |> Option.get; ApiToken = harvestCfg $ "api_token" |> Option.get }
    let harvestHttpClient = getHarvestHttpClient auth
    do printfn "obtain harvest time entries"
    let timeEntries = getTimeEntriesForTimeRange invoiceDateRange harvestHttpClient
    timeEntries

let getReport cfg harvestTimeEntries =
    do printfn "prepare report"
    let reportCfg = cfg $ "report"
    let report = Report.getReport reportCfg harvestTimeEntries
    let formatNum (num: float) = num.ToString("0.00")
    do printfn "reported %s hours, %s total" <| formatNum report.TotalHours <| formatNum report.TotalPrice
    report

let genInvoicePdf cfg report =
    do printfn "prepare template"
    let invoiceDate = invoiceDateRange |> snd |> (fun x -> x.NextDay().BeginningOfDay())
    let invDateStr (format: string) = invoiceDate.ToString(format)
    let templateCfg = cfg $ "template" |> Option.get
    let generatedCfg =
        [
            ("created", invDateStr("dd.MM.yyyy") :> obj)
            ("invoice_number", sprintf "INV%s/1" <| invDateStr("yyyyMMdd") :> obj)
            ("projects", report.Projects :> obj)
            ("total_hours", report.TotalHours :> obj)
            ("total_price", report.TotalPrice :> obj)
        ]
        |> Map.ofSeq
    do templateCfg |> cfgSet "generator" generatedCfg

    do printfn "generate invoice html"
    let templateHtmlPath = templateCfg |> genInvoiceHtml

    do printfn "generate invoice pdf"
    do templateHtmlPath |> genInvoicePdfFromHtml invoiceFilePath


[<EntryPoint>]
let main _args =
    let cfg = Some <| Config.readConfig()
    let frmtDate (date: DateTime) = date.ToString("dd.MM.yyyy")
    let fromDate = invoiceDateRange |> fst |> frmtDate
    let toDate = invoiceDateRange |> snd |> frmtDate
    do printfn "generate invoice for [%s; %s]" <| fromDate <| toDate
    if isInvoiceExists then
        do printfn "invoice already exists!"
    else
        do
            getHarvestTimeEntries cfg
            |> getReport cfg
            |> genInvoicePdf cfg
        do printfn "done!"

    0
