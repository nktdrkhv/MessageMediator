using Telegram.Bot.Types;

namespace MessageMediator.Bot.Abstractions;

public interface IUpdateProcessor
{
    public IUpdateProcessor? Next { get; set; }
    public Task ProcessUpdate(Update update);
}