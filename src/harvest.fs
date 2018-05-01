module Harvest

open FSharpx
open FSharpx.Reader
open OptionConverter
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open Newtonsoft.Json
open System
open System.Collections.Generic

// https://help.getharvest.com/api-v2/tasks-api/tasks/tasks/

type Auth = {
    AccountId: string
    ApiToken: string
}

type TimeReq = {
    [<JsonProperty("from")>] From: DateTime
    [<JsonProperty("to")>] To: DateTime
    [<JsonProperty("page")>] Page: int
}

let makeTimeReq fromDate toDate page =
    { From = fromDate; To = toDate; Page = page }

let timeReqQuery timeReq =
    let toKvp (x, y) = KeyValuePair(x, y)
    let queryParams = [
        ("from", timeReq.From.ToString("u"))
        ("to", timeReq.To.ToString("u"))
        ("page", timeReq.Page.ToString())
    ]
    use content = queryParams |> List.map toKvp |> FormUrlEncodedContent
    content.ReadAsStringAsync().Result


type TimeEntryTask = {
    [<JsonProperty("id")>] Id: int64
    [<JsonProperty("name")>] Name: string
}

type TimeEntry = {
    [<JsonProperty("id")>] Id: int64
    [<JsonProperty("hours")>] Hours: double
    [<JsonProperty("notes")>] Notes: string option
    [<JsonProperty("task")>] Project: TimeEntryTask
}

type TimeResp = {
    [<JsonProperty("time_entries")>] TimeEntries: TimeEntry list
    [<JsonProperty("per_page")>] PerPage: int
    [<JsonProperty("total_pages")>] TotalPages: int
    [<JsonProperty("total_entries")>] TotalEntries: int
    [<JsonProperty("next_page")>] NextPage: int option
    [<JsonProperty("previous_page")>] PrevPage: int option
    [<JsonProperty("page")>] Page: int
}

let harvestAccountIdHeader = "Harvest-Account-ID"
let harvestTimeEntriesUrl query = sprintf "https://api.harvestapp.com/v2/time_entries?%s" query
let jsonMediaType = "application/json"
let userAgent = "invoice_generator"

let getHarvestHttpClient auth =
    let httpClientHandler =
        new HttpClientHandler (
            AutomaticDecompression = DecompressionMethods.GZip
        )
    let httpClient = new HttpClient(httpClientHandler)
    let headers = httpClient.DefaultRequestHeaders
    do headers.UserAgent.ParseAdd(userAgent)
    do headers.Authorization <- AuthenticationHeaderValue("Bearer", auth.ApiToken)
    do headers.Add(harvestAccountIdHeader, auth.AccountId)
    httpClient

let getTimeEntries (req: TimeReq) (httpClient: HttpClient) =
    let reqQuery = timeReqQuery req
    let reqUrl = harvestTimeEntriesUrl reqQuery
    let respMsg = httpClient.GetAsync(reqUrl).Result
    let respJson = respMsg.Content.ReadAsStringAsync().Result
    let resp = JsonConvert.DeserializeObject<TimeResp>(respJson, OptionConverter())
    resp

let getTimeEntriesForTimeRange (fromDate, toDate) = reader {
    let firstPageReq = makeTimeReq fromDate toDate 1
    let! firstPage = getTimeEntries firstPageReq
    // TODO: simplification
    let remainingPageNums = List.init (firstPage.TotalPages - 1) ((+) 2)
    let! remainingPages =
        remainingPageNums
        |> List.map (fun page ->
            let req = { firstPageReq with Page = page }
            getTimeEntries req
        )
        |> Reader.sequence
    let pages = firstPage :: remainingPages
    return pages |> List.collect (fun x -> x.TimeEntries)
}
