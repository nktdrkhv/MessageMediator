using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(11), Replied]
public sealed class RegularReply : MessageHandler
{
    private readonly BotDbContext _context;

    public RegularReply(BotDbContext context)
    {
        _context = context;
    }

    protected override Task HandleAsync(IContainer<Message> cntr)
    {
        throw new NotImplementedException();
    }
}