using MediatR;
using MessageMediator.Bot.Abstractions;
using Telegram.Bot.Types;

namespace MessageMediator.Bot.UpdateProcessors;

public class SupervisorUpdateProcessor : BaseUpdateProcessor
{
    private readonly IMediator _mediator;
    public SupervisorUpdateProcessor(IMediator mediator) => _mediator = mediator;

    public override async Task ProcessMessage(Message message)
    {
        if (message.ReplyToMessage is null || HasPayload(message.Type) is false)
            return;
        // todo: send chain-link back to worker
    }

    public override async Task ProcessCallbackQuery(CallbackQuery callbackQuery)
    {
        // todo: approve means reply worker's message to last source's message in the chain
        // todo: decline means to hide bla bla
    }

    public override async Task ProcessMyChatMember(ChatMemberUpdated chatMemberUpdated)
    {
        // todo: blocked by user -> notify and close
    }
}