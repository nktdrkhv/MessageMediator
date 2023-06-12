using MediatR;
using MessageMediator.Bot.Enums;
using Telegram.Bot.Types;

namespace MessageMediator.Bot.Requests;

public class IdentifyChatRoleRequest : IRequest<ChatRole>
{
    public Update Update { get; private set; }
    public IdentifyChatRoleRequest(Update update) => Update = update;
}