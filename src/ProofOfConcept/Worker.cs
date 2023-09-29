using Telegram.Bot.Polling;
using TelegramUpdater;
using TelegramUpdater.Hosting;

namespace MessageMediator.ProofOfConcept;

public class Worker : UpdateWriterServiceAbs
{
    public Worker(IUpdater updater) : base(updater) { }

    public override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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