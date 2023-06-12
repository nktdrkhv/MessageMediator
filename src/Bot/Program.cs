using MessageMediator.Bot.Configuration;
using MessageMediator.Bot.Data;
using MessageMediator.Bot.Services;
using MessageMediator.Bot.UpdateProcessors;
using Microsoft.Extensions.Options;
using Telegram.Bot;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMemoryCache();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

        services.Configure<BotOptions>(context.Configuration.GetSection(BotOptions.Position));
        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    var configOptions = sp.GetService<IOptions<BotOptions>>()!.Value;
                    var botOptions = new TelegramBotClientOptions(configOptions.ApiToken);
                    return new TelegramBotClient(botOptions, httpClient);
                });

        // services.AddDbContext<AppDbContext>(options =>
        // {
        //
        // });

        services.AddHostedService<PollingService>();
        services.AddScoped<UpdateHandlerService>();
        services.AddScoped<AdminUpdateProcessor>();
        services.AddScoped<SourceUpdateProcessor>();
        services.AddScoped<WorkerUpdateProcessor>();
        services.AddScoped<SupervisorUpdateProcessor>();
        services.AddScoped<NewbyUpdateProcessor>();
    })
    .Build();

await host.RunAsync();