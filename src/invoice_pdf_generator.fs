module InvoicePdfGenerator

open FSharpx
open HandlebarsDotNet
open Config
open System
open System.Text.RegularExpressions

let invoiceTemplateDir = "invoice_template"
let invoiceTemplateName = "index.html"
let invoicePdf = "result.pdf"

let handlebars templateConfig =
    let numbersCfg = templateConfig $ "numbers"
    let numberDecimalSeparator = numbersCfg $ "decimal_separator" |> Option.map string
    let numberGroupSeparator = numbersCfg $ "group_separator" |> Option.map string
    let numberFormatInfo = System.Globalization.NumberFormatInfo()
    do numberDecimalSeparator |> Option.iter (fun x -> numberFormatInfo.NumberDecimalSeparator <- x)
    do numberGroupSeparator |> Option.iter (fun x -> numberFormatInfo.NumberGroupSeparator <- x)

    let cfg = HandlebarsConfiguration()
    do cfg.ThrowOnUnresolvedBindingExpression <- true
    let newlineRegx = new Regex("(\\r\\n|\\n|\\r)", RegexOptions.Multiline);
    let breaklinesHelper = HandlebarsHelper(fun output _context arguments ->
        (
        let str = newlineRegx.Replace(arguments.[0] |> string, "<br>")
        do output.WriteSafeString(str)
        )
    )
    let formatNumberHelper = HandlebarsHelper(fun output _context arguments ->
        (
        let number = Convert.ToDouble(arguments.[0])
        let format = arguments.[1] |> string
        let formattedNumber = number.ToString(format, numberFormatInfo)
        do output.WriteSafeString(formattedNumber)
        )
    )
    do cfg.Helpers.Add("breaklines", breaklinesHelper)
    do cfg.Helpers.Add("formatNumber", formatNumberHelper)

    Handlebars.Create(cfg)

let genInvoiceHtml templateConfig =
    let invoiceTemplatePath = IO.combinePaths invoiceTemplateDir invoiceTemplateName
    let invoiceTemplate = invoiceTemplatePath |> IO.readFileAsString
    let templater = handlebars(Some templateConfig).Compile(invoiceTemplate)
    let invoiceHtml = templater.Invoke(templateConfig)
    invoiceHtml

let genInvoicePdfFromHtml outputPath html =
    use tmpDir = Tmp.cloneInTmpDir(invoiceTemplateDir)

    let invoiceTemplatePath = IO.combinePaths tmpDir.Path invoiceTemplateName
    do IO.writeStringToFile false invoiceTemplatePath html

    do HtmlRasterizer.htmlToPdf invoiceTemplatePath outputPath
