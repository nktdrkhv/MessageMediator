using MediatR;
using MessageMediator.Bot.Abstractions;
using Telegram.Bot.Types;

namespace MessageMediator.Bot.UpdateProcessors;

public class SourceUpdateProcessor : BaseUpdateProcessor
{
    private readonly IMediator _mediator;
    public SourceUpdateProcessor(IMediator mediator) => _mediator = mediator;

    public override async Task ProcessMessage(Message message)
    {
        if (message.ReplyToMessage is var repliedMessage && HasPayload(message.Type))
        {
            // todo: check if it's in chain
            // todo: send reply to worker
        }
        else if (HasPayload(message.Type))
        {
            // todo: check for trigger
            // todo: send new task to worker
        }
    }

    public override Task ProcessCallbackQuery(CallbackQuery callbackQuery) => Task.CompletedTask;

    public override async Task ProcessMyChatMember(ChatMemberUpdated chatMemberUpdated)
    {
        // todo: left group chat or blocked by user -> notify and close the source
    }
}