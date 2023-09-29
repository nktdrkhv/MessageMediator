using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.MyChatMembers;

public sealed class SelfBlockDetection : MyChatMemberHandler
{
    private readonly BotDbContext _context;

    public SelfBlockDetection(BotDbContext context)
    {
        _context = context;
    }

    protected async override Task HandleAsync(IContainer<ChatMemberUpdated> cntr)
    {

    }
}