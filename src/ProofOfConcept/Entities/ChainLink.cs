using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using MessageMediator.ProofOfConcept.Enums;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("ChainLink")]
public class
    ChainLink : ICreatedAt
{
    public ChainLink(Chain motherChain, LocalMessage recievedMessage, LocalMessage forwardedMessage,
        LocalMessage? repliedMessage = null, ChainLink? referenceLink = null, ChainLinkMode? mode = null,
        bool hide = false)
    {
        MotherChain = motherChain;
        Mode = mode ?? referenceLink?.Mode ?? ChainLinkMode.Normal;
        Hide = hide;

        RecievedMessage = recievedMessage;
        recievedMessage.ReferenceTo = repliedMessage;

        ForwardedMessage = forwardedMessage;
        forwardedMessage.ReferenceTo = recievedMessage;
    }

    private ChainLink() { }
    public int Id { get; }
    public ChainLinkMode Mode { get; set; } = ChainLinkMode.Normal;
    public bool Hide { get; set; }

    public int MotherChainId { get; set; }
    public Chain MotherChain { get; set; } = null!;

    public int RecievedMessageId { get; set; }
    public int ForwardMessageId { get; set; }
    public LocalMessage RecievedMessage { get; set; } = null!;
    public LocalMessage ForwardedMessage { get; set; } = null!;

    public TrineRole RoleOfSender => RoleOf(RecievedMessage);
    public TrineRole RoleOfAddressee => RoleOf(ForwardedMessage);
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public bool IsBelongTo(long chatId)
    {
        return MotherChain.SourceChatId == chatId ||
               MotherChain.Worker!.ChatId == chatId ||
               MotherChain.Supervisor?.ChatId == chatId;
    }

    public TrineRole RoleOf(LocalMessage localMessage)
    {
        return RoleOf(localMessage.ChatId);
    }

    public TrineRole RoleOf(long chatId)
    {
        if (chatId == MotherChain.SourceChatId)
        {
            return TrineRole.Source;
        }

        if (chatId == MotherChain.Worker!.ChatId)
        {
            return TrineRole.Worker;
        }

        if (chatId == MotherChain.Supervisor?.ChatId)
        {
            return TrineRole.Supervisor;
        }

        return TrineRole.None;
    }

    public LocalMessage TwinOf(int telegramMessageId)
    {
        return RecievedMessage.TelegramMessageId == telegramMessageId ? ForwardedMessage :
            ForwardedMessage.TelegramMessageId == telegramMessageId ? RecievedMessage :
            throw new ArgumentException("Submitted Telegram message ID are not a part of the chain link.");
    }
}