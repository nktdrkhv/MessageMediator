using System.ComponentModel.DataAnnotations.Schema;
using MessageMediator.ProofOfConcept.Abstract;
using MessageMediator.ProofOfConcept.Enums;

namespace MessageMediator.ProofOfConcept.Entities;

[Table("ChainLink")]
public class ChainLink : ICreatedAt
{
    public int Id { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public int MotherChainId { get; set; }
    public Chain MotherChain { get; set; } = null!;

    public int RecievedMessageId { get; set; }
    public int ForwardMessageId { get; set; }
    public LocalMessage RecievedMessage { get; set; } = null!;
    public LocalMessage ForwardMessage { get; set; } = null!;

    public bool IsBelongTo(long chatId) =>
                (MotherChain.SourceChatId == chatId) ||
                (MotherChain.Worker!.ChatId == chatId) ||
                (MotherChain.Supervisor?.ChatId == chatId);

    public TrineRole RoleOfSender => RoleOf(RecievedMessage);
    public TrineRole RoleOfAddressee => RoleOf(ForwardMessage);

    public TrineRole RoleOf(LocalMessage localMessage) => RoleOf(localMessage.ChatId);

    public TrineRole RoleOf(long chatId)
    {
        if (chatId == MotherChain.SourceChatId)
            return TrineRole.Source;
        if (chatId == MotherChain.Worker!.ChatId)
            return TrineRole.Worker;
        if (chatId == MotherChain.Supervisor?.ChatId)
            return TrineRole.Supervisor;
        return TrineRole.None;
    }

    public LocalMessage TwinOf(int telegramMessageId) => RecievedMessage.TelegramMessageId == telegramMessageId ?
                                                        RecievedMessage :
                                                        ForwardMessage.TelegramMessageId == telegramMessageId ?
                                                        ForwardMessage :
                                                        throw new ArgumentException("Submitted Telegram message ID are not a part of the chain link.");
}