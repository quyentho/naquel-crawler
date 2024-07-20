using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace NaqelExpressCrawl.Controllers;

[ApiController]
[Route("[controller]")]
public class NaqelCrawlingController : ControllerBase
{
    private readonly ILogger<NaqelCrawlingController> _logger;
    private readonly CrawlingService.CrawlingService _service;

    public NaqelCrawlingController(ILogger<NaqelCrawlingController> logger, CrawlingService.CrawlingService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpPost]
    public async Task Get(List<string> referenceNumbers)
    {
            // $"{ShipNo},{PickupDate},{Destination},{PaymentMethod},{ExpectedDeliveryDate},{PieceCount},{StatusDate},{StatusDescription},{StatusLocation},{StatusTime}";
        string csvHeader = "Shipment No, Pickup Date, Destination, Payment Method, Expected Delivery Date, Piece Count,Status Date,Status Description,Status Location,Status Time" + Environment.NewLine;
        
        Response.ContentType = "application/octet-stream";
        Response.Headers.ContentDisposition = "attachment; filename=tracking.csv";
        await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(csvHeader), 0, csvHeader.Length);

        await foreach(var pageContent in _service.GetPageContentAsync(referenceNumbers))
        {
            var result  = _service.ParseContent(pageContent);
            
            foreach (var item in result)
            {
                var line = item.ToString() + Environment.NewLine;
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(line), 0, line.Length);
            }
        }
    }
}