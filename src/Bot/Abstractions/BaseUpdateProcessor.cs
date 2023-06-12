using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MessageMediator.Bot.Abstractions;

public abstract class BaseUpdateProcessor : IUpdateProcessor
{
    protected bool HasPayload(MessageType type) => type is
        MessageType.Text or
        MessageType.Photo or
        MessageType.Video or
        MessageType.Voice or
        MessageType.Document or
        MessageType.Contact or
        MessageType.VideoNote or
        MessageType.Animation;

    public IUpdateProcessor? Next { get; set; }
    public Task GoNext() => Next is not null ? Next.ProcessUpdate(_currentUpdate) : Task.CompletedTask;
    private Update _currentUpdate = null!;

    public virtual async Task ProcessUpdate(Update update)
    {
        _currentUpdate = update;
        var handler = update switch
        {
            { Message: { } message } => ProcessMessage(message),
            { CallbackQuery: { } callbackQuery } => ProcessCallbackQuery(callbackQuery),
            { MyChatMember: { } chatMemberUpdated } => ProcessMyChatMember(chatMemberUpdated),
            _ => Task.CompletedTask
        };
        await handler;
    }

    public abstract Task ProcessMessage(Message message);
    public abstract Task ProcessCallbackQuery(CallbackQuery callbackQuery);
    public abstract Task ProcessMyChatMember(ChatMemberUpdated chatMemberUpdated);
}