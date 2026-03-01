using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace CrawlingService;

public class CrawlingService
{
    private const string BaseUrl = "https://www.naqelexpress.com/en/tracking/";
    private readonly ILogger<CrawlingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public CrawlingService(ILogger<CrawlingService> logger, IHttpClientFactory httpClientFactory)
    {
        this._logger = logger;
        this._httpClientFactory = httpClientFactory;
    }
    //public IEnumerable<TrackingDetails> ParseContent(string pageContent)
    //{
    //    HtmlDocument doc = new();
    //    doc.LoadHtml(pageContent);
    //    var root = doc.DocumentNode;
    //    var generalInfoCardsTask = root.SelectNodes("//div[@class='card-body bg-white']");
    //    var shipmentDetailsCardTask = root.SelectNodes("//div[@class='card-body']");

    //    for (int i = 0; i < generalInfoCardsTask.Count; i++)
    //    {
    //        var infoCard = generalInfoCardsTask[i];
    //        string tdXpath = "(.//table//td)";
    //        var shipNo = infoCard.SelectSingleNode($"{tdXpath}[1]").InnerText;
    //        var destination = infoCard.SelectSingleNode($"{tdXpath}[2]").InnerText;
    //        var expectedDeliveryDate = infoCard.SelectSingleNode($"{tdXpath}[3]").InnerText;
    //        var pickupDate = infoCard.SelectSingleNode($"{tdXpath}[4]").InnerText;
    //        var paymentMethod = infoCard.SelectSingleNode($"{tdXpath}[5]").InnerText;
    //        var pieceCount = infoCard.SelectSingleNode($"{tdXpath}[6]").InnerText;

    //        var detailCard = shipmentDetailsCardTask[i];
    //        var statusRows = detailCard.SelectNodes("./div[@class='row']");
    //        foreach (var row in statusRows)
    //        {
    //            var date = row.SelectSingleNode("./div[1]//p[not(contains(@class,'hide_in_phone'))]").InnerText
    //                .Replace(",", " ");
    //            var timeLineRows = row.SelectNodes("./div[position() > 1]");
    //            foreach (var timeLineRow in timeLineRows)
    //            {
    //                var description = timeLineRow.SelectSingleNode("(.//p)[1]").InnerText;
    //                var location = timeLineRow.SelectSingleNode("(.//p)[2]").InnerText;
    //                var time = timeLineRow.SelectSingleNode("(.//p)[3]").InnerText;

    //                yield return new(shipNo, pickupDate, destination,
    //                    paymentMethod, expectedDeliveryDate,
    //                    pieceCount, date, description, location,
    //                    time);
    //            }
    //        }
    //    }
    //}

    public async IAsyncEnumerable<string> GetPageContentInChunksAsync(IEnumerable<string> referenceNumbers)
    {
        var browserFetcher = new BrowserFetcher();
        var browsers = browserFetcher.GetInstalledBrowsers();
        if (browsers == null || !browsers.Any())
        {
            await browserFetcher.DownloadAsync();
        }

        var currentRunningDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var cookieConsentBlockerFolder = Path.Combine(currentRunningDirectory, "block-cookie-consent");
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = false,
            Args = new string[]
            {
                $"--disable-extensions-except={cookieConsentBlockerFolder}",
                $"--load-extension={cookieConsentBlockerFolder}",
                "--no-sandbox"
            }
        });
        var page = await browser.NewPageAsync();
        await page.GoToAsync(BaseUrl);

        var chunks = referenceNumbers.Chunk(10);
        foreach (var chunk in chunks)
        {
            var form = await page.XPathAsync("//form[textarea[@id='id_waybills']]");
            var inputArea = await page.QuerySelectorAsync("#id_waybills");
            await inputArea.EvaluateFunctionAsync($"el => el.value = '{string.Join(",", chunk)}'");
            await form.First().EvaluateFunctionAsync("e => e.submit()");

            await page.WaitForNavigationAsync();

            yield return await page.GetContentAsync();
        }
    }

    public async Task<string> GetPageContentAsync(IEnumerable<string> referenceNumbers)
    {
        try
        {
            var browserFetcher = new BrowserFetcher();
            var browsers = browserFetcher.GetInstalledBrowsers();
            if (browsers == null || !browsers.Any())
            {
                await browserFetcher.DownloadAsync();
            }

            var currentRunningDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var cookieConsentBlockerFolder = Path.Combine(currentRunningDirectory, "block-cookie-consent");
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new string[]
                {
                $"--disable-extensions-except={cookieConsentBlockerFolder}",
                $"--load-extension={cookieConsentBlockerFolder}",
                "--no-sandbox",
                "--disable-features=site-per-process" // this is required to run multiple instances in different thread
                }
            });
            var page = await browser.NewPageAsync();
            await page.GoToAsync(BaseUrl);

            var form = await page.XPathAsync("//form[textarea[@id='id_waybills']]");
            var inputArea = await page.QuerySelectorAsync("#id_waybills");
            await inputArea.EvaluateFunctionAsync($"el => el.value = '{string.Join(",", referenceNumbers)}'");
            await form.First().EvaluateFunctionAsync("e => e.submit()");

            await page.WaitForNavigationAsync();

            return await page.GetContentAsync();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting page content");
            throw;
        }
    }

    public IEnumerable<TrackingDetails> ParseXmlContent(string xml)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);

        XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
        nsmgr.AddNamespace("tempuri", "http://tempuri.org/");

        XmlNodeList trackNodes = doc.SelectNodes("//tempuri:Tracking", nsmgr);

        foreach (XmlNode trackNode in trackNodes)
        {
            string shipNo = trackNode.SelectSingleNode("tempuri:WaybillNo", nsmgr)?.InnerText ?? string.Empty;
            string statusDate = trackNode.SelectSingleNode("tempuri:Date", nsmgr)?.InnerText ?? string.Empty;
            string statusDescription = trackNode.SelectSingleNode("tempuri:Activity", nsmgr)?.InnerText ?? string.Empty;
            string statusLocation = trackNode.SelectSingleNode("tempuri:StationCode", nsmgr)?.InnerText ?? string.Empty;
            string statusTime = DateTime.Parse(statusDate).ToString("HH:mm:ss");

            yield return new TrackingDetails(
                shipNo,
                statusDate,
                statusDescription,
                statusLocation,
                statusTime
            );
        }
    }

    public async IAsyncEnumerable<IEnumerable<TrackingDetails>> FetchFromNaqelApi(IEnumerable<string> referenceNumbers)
    {
        using var client = _httpClientFactory.CreateClient();

        var chunks = referenceNumbers.Chunk(20);
        foreach (var chunk in chunks)
        {
            var body = GetContent(chunk);
            var content = new StringContent(body, Encoding.UTF8, "text/xml");

            yield return await RequestAndParseContent(client, content);
        }
    }
    
    private async Task<IEnumerable<TrackingDetails>> RequestAndParseContent(HttpClient client, StringContent content)
    {
        Task<IEnumerable<TrackingDetails>> result =  await client.PostAsync("https://infotrack.naqelexpress.com/NaqelAPIServices/NaqelAPI/9.0/XMLShippingService.asmx", content)
        .ContinueWith(async continuation =>
        {
            var response = continuation.Result;
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync().ContinueWith(continuation =>
            {
                return ParseXmlContent(continuation.Result);
            });
        });

        return await result; 
    }

    const string XmlTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <TraceByMultiWaybillNo xmlns=""http://tempuri.org/"">
            <ClientInfo>
                <ClientID>9029519</ClientID>
                <Password>9D$s$A9</Password>
                <Version>9.0</Version>
            </ClientInfo>
      <WaybillNo>
            {0}
      </WaybillNo>
    </TraceByMultiWaybillNo>
  </soap:Body>
</soap:Envelope>";
    private string GetContent(IEnumerable<string> referenceNumbers)
    {
        var sb = new StringBuilder();
        foreach (var refNumber in referenceNumbers)
        {
            sb.Append("<int>").Append(refNumber).Append("</int>");
        }
        return string.Format(XmlTemplate, sb.ToString());
    }
}