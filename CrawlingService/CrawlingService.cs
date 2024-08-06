using System.IO.Compression;
using System.Reflection;
using HtmlAgilityPack;
using PuppeteerSharp;

namespace CrawlingService;

public class CrawlingService
{
    private const string BaseUrl = "https://www.naqelexpress.com/en/tracking/";

    public IEnumerable<TrackingDetails> ParseContent(string pageContent)
    {
        HtmlDocument doc = new();
        doc.LoadHtml(pageContent);
        var root = doc.DocumentNode;
        var generalInfoCardsTask = root.SelectNodes("//div[@class='card-body bg-white']");
        var shipmentDetailsCardTask = root.SelectNodes("//div[@class='card-body']");

        for (int i = 0; i < generalInfoCardsTask.Count; i++)
        {
            var infoCard = generalInfoCardsTask[i];
            string tdXpath = "(.//table//td)";
            var shipNo = infoCard.SelectSingleNode($"{tdXpath}[1]").InnerText;
            var destination = infoCard.SelectSingleNode($"{tdXpath}[2]").InnerText;
            var expectedDeliveryDate = infoCard.SelectSingleNode($"{tdXpath}[3]").InnerText;
            var pickupDate = infoCard.SelectSingleNode($"{tdXpath}[4]").InnerText;
            var paymentMethod = infoCard.SelectSingleNode($"{tdXpath}[5]").InnerText;
            var pieceCount = infoCard.SelectSingleNode($"{tdXpath}[6]").InnerText;

            var detailCard = shipmentDetailsCardTask[i];
            var statusRows = detailCard.SelectNodes("./div[@class='row']");
            foreach (var row in statusRows)
            {
                var date = row.SelectSingleNode("./div[1]//p[not(contains(@class,'hide_in_phone'))]").InnerText
                    .Replace(",", " ");
                var timeLineRows = row.SelectNodes("./div[position() > 1]");
                foreach (var timeLineRow in timeLineRows)
                {
                    var description = timeLineRow.SelectSingleNode("(.//p)[1]").InnerText;
                    var location = timeLineRow.SelectSingleNode("(.//p)[2]").InnerText;
                    var time = timeLineRow.SelectSingleNode("(.//p)[3]").InnerText;

                    yield return new(shipNo, pickupDate, destination,
                        paymentMethod, expectedDeliveryDate,
                        pieceCount, date, description, location,
                        time);
                }
            }
        }
    }

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
}