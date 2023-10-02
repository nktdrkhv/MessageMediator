using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.CallbackQueries;

[Order(10)]
public sealed class RegularReaction : CallbackQueryHandler
{
    private readonly BotDbContext _context;

    public RegularReaction(BotDbContext context)
    {
        _context = context;
    }

    protected override Task HandleAsync(IContainer<CallbackQuery> cntr)
    {
        throw new NotImplementedException();
    }
}