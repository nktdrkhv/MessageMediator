using MediatR;
using MessageMediator.Bot.Abstractions;
using MessageMediator.Bot.Enums;
using MessageMediator.Bot.Requests;
using MessageMediator.Bot.UpdateProcessors;

namespace MessageMediator.Bot.RequestsHandlers;

public class GetUpdateProcessorRequestHandler : IRequestHandler<GetUpdateProcessorRequest, IUpdateProcessor?>
{
    private readonly IServiceProvider _serviceProvider;
    public GetUpdateProcessorRequestHandler(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task<IUpdateProcessor?> Handle(GetUpdateProcessorRequest request, CancellationToken ct)
    {
        var specificHandler = request.ChatRole switch
        {
            ChatRole.Admin => _serviceProvider.GetService<AdminUpdateProcessor>() as IUpdateProcessor,
            ChatRole.Worker => _serviceProvider.GetService<WorkerUpdateProcessor>() as IUpdateProcessor,
            ChatRole.Supervisor => _serviceProvider.GetService<SupervisorUpdateProcessor>() as IUpdateProcessor,
            ChatRole.Source => _serviceProvider.GetService<SourceUpdateProcessor>() as IUpdateProcessor,
            _ => _serviceProvider.GetService<NewbyUpdateProcessor>() as IUpdateProcessor,
        };
        return Task.FromResult(specificHandler);
    }
}