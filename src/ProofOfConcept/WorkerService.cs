using Telegram.Bot.Polling;
using TelegramUpdater;
using TelegramUpdater.Hosting;

namespace MessageMediator.ProofOfConcept;

public class WorkerService : UpdateWriterServiceAbs
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WorkerService(IServiceScopeFactory scopeFactory, IUpdater updater) : base(updater)
    {
        _scopeFactory = scopeFactory;

        updater.AddUserNumericStateKeeper("admin");

        // todo: chat numeric state keeper
        //updater.AddUserNumericStateKeeper("worker");
        //updater.AddUserNumericStateKeeper("supervisor");
        //updater.AddUserNumericStateKeeper("source");
    }

    public override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // using (var scope = _scopeFactory.CreateScope())
        // {
        //     var provider = scope.ServiceProvider;
        //     var db = provider.GetRequiredService<BotDbContext>();
        //     var conf = provider.GetRequiredService<IOptions<BotConfiguration>>().Value;

        //     Updater.TryGetUserNumericStateKeeper("admin", out var keeper);
        //     foreach (var adminId in conf.Administrators)
        //     {
        //         keeper!.SetState(adminId, 0);
        //         var commandsScope = BotCommandScope.Chat(new ChatId(adminId));
        //         await Updater.BotClient.SetMyCommandsAsync(new BotCommand[]
        //         {
        //             new BotCommand() {Command = "sources", Description = "Источники задач"},
        //             new BotCommand() {Command = "workers", Description = "Исполнители"},
        //             new BotCommand() {Command = "supervisors", Description = "Проверяющие"}
        //         }, scope: commandsScope, cancellationToken: stoppingToken);
        //     }
        // }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var reciever = new QueuedUpdateReceiver(Updater.BotClient);
                await foreach (var update in reciever.WithCancellation(stoppingToken))
                    await EnqueueUpdateAsync(update, stoppingToken);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogError(ex, "There was an error, during enqueueing updates");
                await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
            }
        }
    }
}