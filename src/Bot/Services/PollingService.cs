using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MessageMediator.Bot.Services;

public class PollingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PollingService> _logger;
    private readonly ITelegramBotClient _bot;

    public PollingService(IServiceScopeFactory serviceScopeFactory, ILogger<PollingService> logger, ITelegramBotClient bot)
    {
        _logger = logger;
        _bot = bot;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = new[] {
                UpdateType.CallbackQuery,
                UpdateType.Message,
                UpdateType.MyChatMember},
            ThrowPendingUpdates = false
        };
        var updateReceiver = new QueuedUpdateReceiver(_bot, receiverOptions);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var update in updateReceiver.WithCancellation(stoppingToken))
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var updateHandler = scope.ServiceProvider.GetRequiredService<UpdateHandlerService>();
                    await updateHandler.Handle(update);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error [{Message}] happened at\n{Empty}", e!.Message, e!.StackTrace);
                await Task.Delay(10000);
            }
        }
    }
}