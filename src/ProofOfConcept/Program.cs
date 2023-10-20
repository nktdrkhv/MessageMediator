using MessageMediator.ProofOfConcept;
using MessageMediator.ProofOfConcept.Configuration;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramUpdater;
using TelegramUpdater.Hosting;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<BotConfiguration>(context.Configuration.GetSection(BotConfiguration.Section));
        services.AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                BotConfiguration configuration = sp.GetService<IOptions<BotConfiguration>>()!.Value;
                TelegramBotClientOptions clientOptions = new TelegramBotClientOptions(
                    configuration.ApiToken,
                    configuration.BaseUrl,
                    configuration.IsTestEnv);
                return new TelegramBotClient(clientOptions, httpClient);
            });
        services.AddTelegramUpdater<WorkerService>(
            new UpdaterOptions(
                32,
                flushUpdatesQueue: true,
                allowedUpdates: Array.Empty<UpdateType>()),
            builder => builder
                .AutoCollectScopedHandlers()
                .AddDefaultExceptionHandler());
        services.AddDbContext<BotDbContext>(options =>
        {
            string? connection = context.Configuration.GetConnectionString("SQLite");
            options.UseSqlite(connection);
        });
        services.AddHostedService<WorkerService>();
    })
    .Build();

host.Run();