using System.Text;
using CrawlingService;
using Microsoft.AspNetCore.Mvc;

namespace NaqelExpressCrawl.Controllers;

[ApiController]
[Route("[controller]")]
public class NaqelCrawlingController : ControllerBase
{
    private readonly ILogger<NaqelCrawlingController> _logger;
    private readonly CrawlingService.CrawlingService _service;
    const int ChunkSize = 10;

    public NaqelCrawlingController(ILogger<NaqelCrawlingController> logger, CrawlingService.CrawlingService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult> Get(List<string> referenceNumbers)
    {
        // $"{ShipNo},{PickupDate},{Destination},{PaymentMethod},{ExpectedDeliveryDate},{PieceCount},{StatusDate},{StatusDescription},{StatusLocation},{StatusTime}";
        string csvHeader =
            "Shipment No, Pickup Date, Destination, Payment Method, Expected Delivery Date, Piece Count,Status Date,Status Description,Status Location,Status Time" +
            Environment.NewLine;

        Response.ContentType = "text/csv";
        Response.Headers.ContentDisposition = "attachment; filename=tracking.csv";
        await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(csvHeader), 0, Encoding.UTF8.GetByteCount(csvHeader));


        var resultString = new StringBuilder();
        resultString.Append(csvHeader);

        var pageContentTasks = referenceNumbers.Chunk(ChunkSize).Select(chunk => _service.GetPageContentAsync(chunk)).ToList();

        while (pageContentTasks.Any())
        {
            var completedTask = await Task.WhenAny(pageContentTasks);
            pageContentTasks.Remove(completedTask);
            var pageContent = await completedTask;
            var result = _service.ParseContent(pageContent);
            foreach (var item in result)
            {
                var line = item.ToString() + "\n";
                var lineBytes = Encoding.UTF8.GetBytes(line);
                await Response.Body.WriteAsync(lineBytes, 0, lineBytes.Length);
            }
        }
        
        return new EmptyResult();
    }
}