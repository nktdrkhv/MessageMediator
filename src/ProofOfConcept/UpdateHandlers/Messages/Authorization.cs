using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(100), Command(prefix: '/', argumentsMode: ArgumentsMode.Require, "start")]
public sealed class Authorization : MessageHandler
{
    private readonly BotDbContext _context;

    public Authorization(BotDbContext context) => _context = context;

    protected override async Task HandleAsync(IContainer<Message> cntr)
    {
        if (cntr.Update.Chat.Type == ChatType.Private && cntr.TryParseCommandArgs(out string? args) && args != null)
        {

        }
        StopPropagation();
    }
}