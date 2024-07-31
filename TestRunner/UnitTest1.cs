namespace TestRunner;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        CrawlingService.CrawlingService service = new();
        var referenceNumbers = new List<string>()
        {
            "288082838",
            "288082841"
        };


        // var tasks = new List<Task<string>>();
        // foreach (var refNum in referenceNumbers)
        // {
        //     tasks.Add(Task.Run(() => service.Test(referenceNumbers)));
        // }
        //
        // var result = await Task.WhenAll(tasks);
    }
}