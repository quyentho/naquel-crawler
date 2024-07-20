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

        var result = new List<TrackingDetails>();

        for (int i = 0; i < generalInfoCardsTask.Count; i++)
        {
            var infoCard = generalInfoCardsTask[i];
            string tdXpath = "(//table//td)";
            var shipNoTask = infoCard.SelectSingleNode($"{tdXpath}[1]").InnerText;
            var destinationTask = infoCard.SelectSingleNode($"{tdXpath}[2]").InnerText;
            var expectedDeliveryDateTask = infoCard.SelectSingleNode($"{tdXpath}[3]").InnerText;
            var pickupDateTask = infoCard.SelectSingleNode($"{tdXpath}[4]").InnerText;
            var paymentMethodTask = infoCard.SelectSingleNode($"{tdXpath}[5]").InnerText;
            var pieceCountTask = infoCard.SelectSingleNode($"{tdXpath}[6]").InnerText;

            var detailCard = shipmentDetailsCardTask[i];
            var statusRows = detailCard.SelectNodes("./div[@class='row']");
            foreach (var row in statusRows)
            {
                var dateTask = row.SelectSingleNode("./div[1]//p[not(contains(@class,'hide_in_phone'))]").InnerText;
                var descriptionTask = row.SelectSingleNode("(./div[position() > 1]//p)[1]").InnerText;
                var locationTask = row.SelectSingleNode("(./div[position() > 1]//p)[2]").InnerText;
                var timeTask = row.SelectSingleNode("(./div[position() > 1]//p)[3]").InnerText;

                yield return new(shipNoTask, pickupDateTask, destinationTask,
                    paymentMethodTask, expectedDeliveryDateTask,
                    pieceCountTask, dateTask, descriptionTask, locationTask,
                    timeTask);
            }
        }
    }

    public async IAsyncEnumerable<string> GetPageContentAsync(IEnumerable<string> referenceNumbers)
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
                $"--load-extension={cookieConsentBlockerFolder}"
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
}