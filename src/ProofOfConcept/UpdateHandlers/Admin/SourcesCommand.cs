using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using MessageMediator.ProofOfConcept.Enums;
using Telegram.Bot.Types;
using TelegramUpdater.FilterAttributes.Attributes;
using TelegramUpdater.Filters;
using TelegramUpdater.Helpers;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(1), Command("source", '/', ArgumentsMode.NoArgs), ChatType(ChatTypeFlags.Private)]
public sealed class SourcesCommand : MessageHandler
{
    private readonly BotDbContext _context;

    public SourcesCommand(BotDbContext context) => _context = context;

    protected override async Task HandleAsync(IContainer<Message> cntr)
    {
        var cmdMsg = cntr.Update;
        var sources = _context.Sources.Where(s => !s.IsDeleted).Select(s => Tuple.Create(s.Alias!, s.Id)).ToArray();
        var msgResp = await SendTextMessageAsync("Выберите источник:",
            replyMarkup: InlineKeyboardMarkupWrapper.FromCustomSet(sources, "source"));
        try
        {
            var chosenSource = await AwaitButtonClickAsync(
                timeOut: TimeSpan.FromSeconds(10),
                callbackQueryRegex: new("source:\\d+"));
            await chosenSource!.AnswerAsync("Got ya");
        }
        catch
        {
            await cntr.ResponseAsync("Время ответа истекло");
        }
        StopPropagation();
    }
}