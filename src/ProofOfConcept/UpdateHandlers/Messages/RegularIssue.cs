using System.Diagnostics.Contracts;
using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(12)]
public sealed class RegularIssue : MessageHandler
{
    private readonly BotDbContext _context;

    public RegularIssue(BotDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleAsync(IContainer<Message> cntr)
    {
        var text = cntr.Update.Text ?? cntr.Update.Caption;
        if (text == null)
            StopPropagation();

    }
}