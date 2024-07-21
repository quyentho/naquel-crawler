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

    public NaqelCrawlingController(ILogger<NaqelCrawlingController> logger, CrawlingService.CrawlingService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult> Get(List<string> referenceNumbers)
    {
            // $"{ShipNo},{PickupDate},{Destination},{PaymentMethod},{ExpectedDeliveryDate},{PieceCount},{StatusDate},{StatusDescription},{StatusLocation},{StatusTime}";
        string csvHeader = "Shipment No, Pickup Date, Destination, Payment Method, Expected Delivery Date, Piece Count,Status Date,Status Description,Status Location,Status Time" + Environment.NewLine;
        
        var resultString = new StringBuilder();
        resultString.Append(csvHeader);
       await foreach(var pageContent in _service.GetPageContentAsync(referenceNumbers))
        {
            var result  = _service.ParseContent(pageContent);
            
            foreach (var item in result)
            {
                var line = item.ToString() + Environment.NewLine;
                resultString.Append(line);
            }
        }

        var test = Encoding.UTF8.GetBytes(resultString.ToString());

        return File(test, "text/csv", "tracking.csv");
    }
}