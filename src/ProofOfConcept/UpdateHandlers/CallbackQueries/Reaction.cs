using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.CallbackQueries;

public sealed class Reaction : CallbackQueryHandler
{
    private readonly BotDbContext _context;

    public Reaction(BotDbContext context)
    {
        _context = context;
    }

    protected async override Task HandleAsync(IContainer<CallbackQuery> cntr)
    {

    }
}