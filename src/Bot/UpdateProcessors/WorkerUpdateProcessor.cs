using MessageMediator.Bot.Abstractions;
using MediatR;
using Telegram.Bot.Types;

namespace MessageMediator.Bot.UpdateProcessors;

public class WorkerUpdateProcessor : BaseUpdateProcessor
{
    private readonly IMediator _mediator;
    public WorkerUpdateProcessor(IMediator mediator) => _mediator = mediator;

    public override async Task ProcessMessage(Message message)
    {
        if (message.ReplyToMessage is null || HasPayload(message.Type) is false)
        {
            await GoNext();
            return;
        }

        // todo: send reply as chain-link to source or supervisor
    }

    public override Task ProcessCallbackQuery(CallbackQuery callbackQuery) => Task.CompletedTask;

    public override async Task ProcessMyChatMember(ChatMemberUpdated chatMemberUpdated)
    {
        // todo: blocked by user -> notify and close
    }
}