using MediatR;
using MessageMediator.Bot.Enums;
using MessageMediator.Bot.Requests;
using Telegram.Bot.Types;

namespace MessageMediator.Bot.RequestsHandlers;

public class IdentifyChatRoleHandler : IRequestHandler<IdentifyChatRoleRequest, ChatRole>
{
    public async Task<ChatRole> Handle(IdentifyChatRoleRequest request, CancellationToken ct)
    {
        // its ChatRole.Admin if Update is MyChatUpdate 
        return ChatRole.Source;
    }
}