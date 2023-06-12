using MediatR;
using MessageMediator.Bot.Abstractions;
using MessageMediator.Bot.Enums;

namespace MessageMediator.Bot.Requests;

public class GetUpdateProcessorRequest : IRequest<IUpdateProcessor?>
{
    public ChatRole ChatRole { get; private set; }
    public GetUpdateProcessorRequest(ChatRole chatRole) => ChatRole = chatRole;
}