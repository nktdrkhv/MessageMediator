using MediatR;
using MessageMediator.Bot.Abstractions;
using Telegram.Bot.Types;

namespace MessageMediator.Bot.UpdateProcessors;

public class AdminUpdateProcessor : BaseUpdateProcessor
{
    private readonly IMediator _mediator;
    public AdminUpdateProcessor(IMediator mediator) => _mediator = mediator;

    public override async Task ProcessMessage(Message message)
    {
        // todo: show /admin-panel
        //await GoNext();
    }

    public override async Task ProcessCallbackQuery(CallbackQuery callbackQuery)
    {
        // todo: group approve or decline (after newby's mychatmember)
        // todo: watch specific source's (exsm. group) workers /list + deep-links for roles enters + delete source or smth
        // todo: watch concrete worker/supervisor + delete + watch history
    }

    public override Task ProcessMyChatMember(ChatMemberUpdated chatMemberUpdated) => Task.CompletedTask;
}