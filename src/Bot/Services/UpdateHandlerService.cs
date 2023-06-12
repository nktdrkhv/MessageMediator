using MediatR;
using MessageMediator.Bot.Requests;
using Telegram.Bot.Types;

namespace MessageMediator.Bot.Services;

public class UpdateHandlerService
{
    private readonly ILogger<UpdateHandlerService> _logger;
    private readonly IMediator _mediator;

    public UpdateHandlerService(ILogger<UpdateHandlerService> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Handle(Update update)
    {
        var role = await _mediator.Send(new IdentifyChatRoleRequest(update));
        var updateProcessor = await _mediator.Send(new GetUpdateProcessorRequest(role));
        await updateProcessor!.ProcessUpdate(update);
    }
}