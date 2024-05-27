#region EventStoreBasicExample

//using EventStore.Client;
//using System.Text.Json;

//string connectionString = "esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false";
//EventStoreClientSettings settings = EventStoreClientSettings.Create(connectionString);
//EventStoreClient client = new EventStoreClient(settings);

//OrderPlacedEvent @event = new()
//{
//	OrderId = 1,
//	TotalAmount = 1000
//};

//#region Event.Insert
//EventData eventData = new(
//	eventId: Uuid.NewUuid(),
//	type: @event.GetType().Name,
//	data: JsonSerializer.SerializeToUtf8Bytes(@event)
//	);

//await client.AppendToStreamAsync(
//	streamName: "order-stream",
//	expectedState: StreamState.Any,
//	eventData: new[] { eventData }
//	);
//#endregion

//#region Event.Get
//var events = client.ReadStreamAsync(
//	streamName: "order-stream",
//	direction: Direction.Forwards,
//	revision: StreamPosition.Start
//	);

//var datas = await events.ToListAsync();
//Console.WriteLine();
//#endregion

//await client.SubscribeToStreamAsync(
//	streamName: "order-stream",
//	start: FromStream.Start,
//	eventAppeared: async (streamSubscription, resolvedEvent, cancellationToken) =>
//	{
//		OrderPlacedEvent @event = JsonSerializer.Deserialize<OrderPlacedEvent>(resolvedEvent.Event.Data.ToArray());
//		await Console.Out.WriteLineAsync(JsonSerializer.Serialize(@event));
//	},
//	subscriptionDropped: (streamSubsctiption, subscriptionDroppedReason, exception) => Console.WriteLine("Disconnected!")
//	);

//Console.ReadLine();

//class OrderPlacedEvent
//{
//	public int OrderId { get; set; }
//	public int TotalAmount { get; set; }
//}

#endregion

#region BankAccountExample

using EventStore.Client;
using System.Text.Json;

EventStoreService eventStoreService = new();

AccountCreatedEvent accountCreatedEvent = new()
{
	AccountId = "12345",
	CostumerId = "98765",
	StartBalance = 0,
	Date = DateTime.UtcNow.Date
};
MoneyDepositedEvent moneyDepositedEvent1 = new()
{
	AccountId = "12345",
	Amount = 1000,
	Date = DateTime.UtcNow.Date
};
MoneyDepositedEvent moneyDepositedEvent2 = new()
{
	AccountId = "12345",
	Amount = 500,
	Date = DateTime.UtcNow.Date
};
MoneyWithdrawnEvent moneyWithdrawnEvent = new()
{
	AccountId = "12345",
	Amount = 200,
	Date = DateTime.UtcNow.Date
};
MoneyDepositedEvent moneyDepositedEvent3 = new()
{
	AccountId = "12345",
	Amount = 50,
	Date = DateTime.UtcNow.Date
};
MoneyTransferredEvent moneyTransferredEvent1 = new()
{
	AccountId = "12345",
	Amount = 250,
	Date = DateTime.UtcNow.Date
};
MoneyTransferredEvent moneyTransferredEvent2 = new()
{
	AccountId = "12345",
	Amount = 150,
	Date = DateTime.UtcNow.Date
};
MoneyDepositedEvent moneyDepositedEvent4 = new()
{
	AccountId = "12345",
	Amount = 2000,
	Date = DateTime.UtcNow.Date
};


//await eventStoreService.AppendToStreamAsync(
//	streamName: $"customer-{accountCreatedEvent.CostumerId}-stream",
//	eventData: new[]  
//	{
//		eventStoreService.GenerateEventData(accountCreatedEvent),
//		eventStoreService.GenerateEventData(moneyDepositedEvent1),
//		eventStoreService.GenerateEventData(moneyDepositedEvent2),
//		eventStoreService.GenerateEventData(moneyWithdrawnEvent),
//		eventStoreService.GenerateEventData(moneyDepositedEvent3),
//		eventStoreService.GenerateEventData(moneyTransferredEvent1),
//		eventStoreService.GenerateEventData(moneyTransferredEvent2),
//		eventStoreService.GenerateEventData(moneyDepositedEvent4),
//	}
//	);

BalanceInfo balanceInfo = new();
await eventStoreService.SubscribeToStreamAsync(
	streamName: $"customer-{accountCreatedEvent.CostumerId}-stream",
	async (ss, re, ct) =>
	{
		string eventType = re.Event.EventType;
		object @event = JsonSerializer.Deserialize(re.Event.Data.ToArray(), Type.GetType(eventType));

		switch (@event)
		{
			case AccountCreatedEvent e:
				balanceInfo.AccountId = e.AccountId;
				balanceInfo.Balance = e.StartBalance;
				break;
			case MoneyDepositedEvent e:
				balanceInfo.Balance += e.Amount;
				break;
			case MoneyWithdrawnEvent e:
				balanceInfo.Balance -= e.Amount;
				break;
			case MoneyTransferredEvent e:
				balanceInfo.Balance -= e.Amount;
				break;
		}

		await Console.Out.WriteLineAsync("------BALANCE-------");
		await Console.Out.WriteLineAsync(JsonSerializer.Serialize(balanceInfo));
		await Console.Out.WriteLineAsync("------BALANCE-------");
		await Console.Out.WriteLineAsync("");
		await Console.Out.WriteLineAsync("");
		await Console.Out.WriteLineAsync("");

	});

Console.Read();

class EventStoreService
{
	EventStoreClientSettings GetEventStoreClientSettings(string connectionString = "esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false") => EventStoreClientSettings.Create(connectionString);

	EventStoreClient Client { get => new EventStoreClient(GetEventStoreClientSettings()); }

	public async Task AppendToStreamAsync(string streamName, IEnumerable<EventData> eventData) => await Client.AppendToStreamAsync(
		streamName: streamName,
		eventData: eventData,
		expectedState: StreamState.Any
		);

	public EventData GenerateEventData(object @event) => new(
		eventId: Uuid.NewUuid(),
		type: @event.GetType().Name,
		data: JsonSerializer.SerializeToUtf8Bytes(@event)
		);

	public async Task SubscribeToStreamAsync(string streamName, Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared)
		=> await Client.SubscribeToStreamAsync(
		streamName: streamName,
		start: FromStream.Start,
		eventAppeared: eventAppeared,
		subscriptionDropped: (x, y, z) => Console.WriteLine("Disconnected!")
		);
}

class BalanceInfo
{
	public string AccountId { get; set; }
	public int Balance { get; set; }
}

class AccountCreatedEvent
{
	public string AccountId { get; set; }
	public string CostumerId { get; set; }
	public int StartBalance { get; set; }
	public DateTime Date { get; set; }
}

class MoneyDepositedEvent
{
	public string AccountId { get; set; }
	public int Amount { get; set; }
	public DateTime Date { get; set; }
}

class MoneyWithdrawnEvent
{
	public string AccountId { get; set; }
	public int Amount { get; set; }
	public DateTime Date { get; set; }
}

class MoneyTransferredEvent
{
	public string AccountId { get; set; }
	public string TargetAccountId { get; set; }
	public int Amount { get; set; }
	public DateTime Date { get; set; }
}
#endregion