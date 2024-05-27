using EventStore.Client;
using System.Text.Json;

string connectionString = "esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false";
EventStoreClientSettings settings = EventStoreClientSettings.Create(connectionString);
EventStoreClient client = new EventStoreClient(settings);

OrderPlacedEvent @event = new()
{
    OrderId = 1,
    TotalAmount = 1000
};

#region Event.Insert
EventData eventData = new(
    eventId: Uuid.NewUuid(),
    type: @event.GetType().Name,
    data: JsonSerializer.SerializeToUtf8Bytes(@event)
    );

await client.AppendToStreamAsync(
    streamName: "order-stream",
    expectedState: StreamState.Any,
    eventData: new[] { eventData }
    );
#endregion

#region Event.Get
var events = client.ReadStreamAsync(
    streamName: "order-stream",
    direction: Direction.Forwards,
    revision: StreamPosition.Start
    );

var datas = await events.ToListAsync();
Console.WriteLine();  
#endregion

class OrderPlacedEvent
{
    public int OrderId { get; set; }
    public int TotalAmount { get; set; }
}
