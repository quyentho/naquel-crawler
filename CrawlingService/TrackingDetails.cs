namespace CrawlingService;

public record TrackingDetails(
    string ShipNo,
    string PickupDate,
    string Destination,
    string PaymentMethod,
    string ExpectedDeliveryDate,
    string PieceCount,
    string StatusDate,
    string StatusDescription,
    string StatusLocation,
    string StatusTime)
{
    public override string ToString()
    {
        return
            $"{ShipNo},{PickupDate},{Destination},{PaymentMethod},{ExpectedDeliveryDate},{PieceCount},{StatusDate},{StatusDescription},{StatusLocation},{StatusTime}";
    }
};