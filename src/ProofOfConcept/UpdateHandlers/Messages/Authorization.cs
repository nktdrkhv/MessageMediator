using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.Helpers;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(100), Command(prefix: '/', argumentsMode: ArgumentsMode.Idc, "start"), ChatType(ChatTypeFlags.Private)]
public sealed class Authorization : MessageHandler
{
    private readonly BotDbContext _context;

    public Authorization(BotDbContext context) => _context = context;

    protected override async Task HandleAsync(IContainer<Message> cntr)
    {
        if (cntr.TryParseCommandArgs(out string? args) && args != null)
        {
            await cntr.ResponseAsync(args);
        }
        else
        {
            await _context.GetOrCreateLocalUserAsync(cntr.Sender()!);
            await cntr.ResponseAsync("Мы любим любопытных");
        }
        StopPropagation();
    }
}