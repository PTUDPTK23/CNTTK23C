using System.Threading.Channels;

// MepDemo - Minh hoa 5 mo hinh trao doi thong diep (MEP) trong mot tien trinh.
// Chay tat ca: dotnet run
// Chay rieng:  dotnet run -- 3   (1..5)

Console.OutputEncoding = System.Text.Encoding.UTF8;
var selected = args.Length > 0 ? args[0] : "all";

if (selected is "all" or "1") await Demo1_RequestResponse();
if (selected is "all" or "2") await Demo2_OneWay();
if (selected is "all" or "3") await Demo3_QueueBased();
if (selected is "all" or "4") Demo4_PubSub();
if (selected is "all" or "5") await Demo5_Streaming();

static async Task Demo1_RequestResponse()
{
    PrintHeader("1. Request-Response: gui yeu cau va CHO phan hoi");
    var grader = new GradingService();

    var result = await grader.GradeAsync("dtc245200452", latencyMs: 250);
    Console.WriteLine($"[Client] Nhan phan hoi: {result}");

    try
    {
        // Gia lap server xu ly lau hon nguong timeout phia client
        await grader.GradeAsync("SV002", latencyMs: 1500).WaitAsync(TimeSpan.FromMilliseconds(600));
    }
    catch (TimeoutException)
    {
        Console.WriteLine("[Client] Qua thoi gian cho 600ms -> bao loi hoac thu lai sau");
    }
}

static async Task Demo2_OneWay()
{
    PrintHeader("2. One-Way (Fire & Forget): gui di va khong cho ket qua");
    var logChannel = Channel.CreateUnbounded<string>();

    var auditWorker = Task.Run(async () =>
    {
        await foreach (var entry in logChannel.Reader.ReadAllAsync())
        {
            await Task.Delay(120);
            Console.WriteLine($"   [AuditLog] da ghi: {entry}");
        }
    });

    for (var i = 1; i <= 3; i++)
    {
        logChannel.Writer.TryWrite($"dtc245200452 - su kien #{i}");
        Console.WriteLine($"[Sender] da gui log #{i} va tiep tuc xu ly ngay");
    }

    logChannel.Writer.Complete();
    await auditWorker;
}

static async Task Demo3_QueueBased()
{
    PrintHeader("3. Queue-Based (Competing Consumers): day job vao hang doi, worker chia viec");
    var queue = Channel.CreateBounded<GradeJob>(capacity: 10);

    async Task RunWorkerAsync(string workerName)
    {
        await foreach (var job in queue.Reader.ReadAllAsync())
        {
            Console.WriteLine($"   [{workerName}] bat dau cham {job.SubmissionId}");
            await Task.Delay(300);
            Console.WriteLine($"   [{workerName}] hoan tat {job.SubmissionId}");
        }
    }

    var workers = new[] { RunWorkerAsync("Worker-A"), RunWorkerAsync("Worker-B") };

    for (var i = 1; i <= 4; i++)
    {
        var job = new GradeJob($"sub-{i}");
        await queue.Writer.WriteAsync(job);
        Console.WriteLine($"[API] nhan {job.SubmissionId} -> dua vao hang doi (status = processing)");
    }

    queue.Writer.Complete();
    await Task.WhenAll(workers);
}

static void Demo4_PubSub()
{
    PrintHeader("4. Publish-Subscribe: mot su kien -> nhieu dich vu cung nhan");
    var eventBus = new InMemoryEventBus();

    eventBus.Subscribe<SubmissionGraded>(e =>
        Console.WriteLine($"   [ScoreService] luu diem {e.Score} cho {e.SubmissionId}"));
    eventBus.Subscribe<SubmissionGraded>(e =>
        Console.WriteLine($"   [NotificationService] thong bao sinh vien: {e.SubmissionId} da co diem"));
    eventBus.Subscribe<SubmissionGraded>(e =>
        Console.WriteLine($"   [StatsService] cap nhat thong ke cho {e.SubmissionId}"));

    Console.WriteLine("[Worker] cham xong -> phat su kien SubmissionGraded");
    eventBus.Publish(new SubmissionGraded("sub-1", 9.0));
}

static async Task Demo5_Streaming()
{
    PrintHeader("5. Streaming / Push: nhan cap nhat trang thai lien tuc");
    await foreach (var statusUpdate in WatchSubmissionAsync("sub-1"))
    {
        Console.WriteLine($"[Client] cap nhat: {statusUpdate}");
    }
}

static async IAsyncEnumerable<string> WatchSubmissionAsync(string submissionId)
{
    var stages = new[] { "received", "processing 30%", "processing 70%", "graded: 9.0" };
    foreach (var stage in stages)
    {
        await Task.Delay(250);
        yield return $"{submissionId} -> {stage}";
    }
}

static void PrintHeader(string title)
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 68));
    Console.WriteLine(title);
    Console.WriteLine(new string('=', 68));
}

sealed class GradingService
{
    public async Task<string> GradeAsync(string studentCode, int latencyMs)
    {
        Console.WriteLine($"   [GradingService] dang cham bai cua {studentCode}...");
        await Task.Delay(latencyMs);
        return $"{studentCode}: 9.0 diem";
    }
}

sealed class InMemoryEventBus
{
    private readonly Dictionary<Type, List<Action<object>>> _subscribers = new();

    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (!_subscribers.TryGetValue(typeof(TEvent), out var list))
        {
            list = new List<Action<object>>();
            _subscribers[typeof(TEvent)] = list;
        }

        list.Add(evt => handler((TEvent)evt));
    }

    public void Publish<TEvent>(TEvent evt) where TEvent : notnull
    {
        if (!_subscribers.TryGetValue(typeof(TEvent), out var list)) return;
        foreach (var handler in list)
        {
            handler(evt);
        }
    }
}

record GradeJob(string SubmissionId);
record SubmissionGraded(string SubmissionId, double Score);
