using MessageMediator.ProofOfConcept.Configuration;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TelegramUpdater;
using TelegramUpdater.Hosting;
using TelegramUpdater.StateKeeping.StateKeepers.NumericStateKeepers;

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
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            IServiceProvider provider = scope.ServiceProvider;
            BotConfiguration conf = provider.GetRequiredService<IOptions<BotConfiguration>>().Value;
            BotDbContext db = provider.GetRequiredService<BotDbContext>();

            Updater.TryGetUserNumericStateKeeper("admin", out UserNumericStateKeeper? keeper);
            foreach (long adminId in conf.Administrators)
            {
                keeper!.SetState(adminId, 0);
                BotCommandScopeChat commandsScope = BotCommandScope.Chat(new ChatId(adminId));
                await Updater.BotClient.SetMyCommandsAsync(
                    new BotCommand[] { new() { Command = "issues", Description = "Распределение задач" } },
                    commandsScope, cancellationToken: stoppingToken);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                QueuedUpdateReceiver reciever = new QueuedUpdateReceiver(Updater.BotClient);
                await foreach (Update update in reciever.WithCancellation(stoppingToken))
                {
                    await EnqueueUpdateAsync(update, stoppingToken);
                }
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