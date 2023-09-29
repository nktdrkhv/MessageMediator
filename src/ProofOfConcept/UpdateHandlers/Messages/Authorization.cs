using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(0), Command(prefix: '/', argumentsMode: ArgumentsMode.NoArgs, "start")]
public sealed class Authorization : MessageHandler
{
    private readonly BotDbContext _context;

    public Authorization(BotDbContext context)
    {
        _context = context;
    }

    protected async override Task HandleAsync(IContainer<Message> cntr)
    {

    }
}