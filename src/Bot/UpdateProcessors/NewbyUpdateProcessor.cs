using MediatR;
using MessageMediator.Bot.Abstractions;
using Telegram.Bot.Types;

namespace MessageMediator.Bot.UpdateProcessors;

public class NewbyUpdateProcessor : BaseUpdateProcessor
{
    private readonly IMediator _mediator;
    public NewbyUpdateProcessor(IMediator mediator) => _mediator = mediator;

    public override Task ProcessMessage(Message message) => Task.CompletedTask;

    public override Task ProcessCallbackQuery(CallbackQuery callbackQuery) => Task.CompletedTask;

    public override async Task ProcessMyChatMember(ChatMemberUpdated chatMemberUpdated)
    {
        // todo: send approve message to admin
    }
}