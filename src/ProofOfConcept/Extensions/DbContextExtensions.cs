using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class DbContextExtensions
{
    public static async ValueTask<LocalUser> GetOrCreateLocalUserAsync(this BotDbContext context, User user)
    {
        if (await context.LocalUsers.FindAsync(user.Id) is LocalUser localUser)
        {
            return localUser;
        }

        LocalUser newUser = new LocalUser(user);
        await context.LocalUsers.AddAsync(newUser);
        await context.SaveChangesAsync();
        return newUser;
    }

    public static async ValueTask<LocalChat> GetOrCreateLocalChatAsync(this BotDbContext context, Chat chat)
    {
        if (await context.LocalChats.FindAsync(chat.Id) is LocalChat localChat)
        {
            return localChat;
        }

        LocalChat newChat = new LocalChat(chat);
        await context.LocalChats.AddAsync(newChat);
        await context.SaveChangesAsync();
        return newChat;
    }

    public static IQueryable<ChainLink> IncludeChainLinkParts(this IQueryable<ChainLink> query)
    {
        return query
            .Include(cl => cl.MotherChain.SourceChat)
            .Include(cl => cl.MotherChain.Worker)
            .Include(cl => cl.MotherChain.Supervisor)
            .Include(cl => cl.MotherChain.Trigger.Source)
            .Include(cl => cl.ForwardedMessage.ReferenceTo)
            .Include(cl => cl.RecievedMessage.ReferenceTo)
            .AsSplitQuery();
    }

    public static IQueryable<ChainLink> WhereLinkFits(this IQueryable<ChainLink> query, LocalMessage message,
        bool checkVisibility)
    {
        return checkVisibility
            ? query.Where(cl => cl.ForwardedMessage == message || cl.RecievedMessage == message)
                .Where(cl => !cl.Hide || cl.ForwardedMessage.ForceShow || cl.RecievedMessage.ForceShow)
                .Where(cl => cl.MotherChain.FinishedAt == null)
            : query.Where(cl => cl.ForwardedMessage == message || cl.RecievedMessage == message)
                .Where(cl => cl.MotherChain.FinishedAt == null);
    }

    public static IQueryable<ChainLink> WhereLinkFits(this IQueryable<ChainLink> query, Message message,
        bool checkVisibility)
    {
        return checkVisibility
            ? query.Where(cl =>
                    (cl.ForwardedMessage.TelegramMessageId == message.MessageId &&
                     cl.ForwardedMessage.ChatId == message.Chat.Id) ||
                    (cl.RecievedMessage.TelegramMessageId == message.MessageId &&
                     cl.RecievedMessage.ChatId == message.Chat.Id))
                .Where(cl => !cl.Hide || cl.ForwardedMessage.ForceShow || cl.RecievedMessage.ForceShow)
                .Where(cl => cl.MotherChain.FinishedAt == null)
            : query.Where(cl =>
                (cl.ForwardedMessage.TelegramMessageId == message.MessageId &&
                 cl.ForwardedMessage.ChatId == message.Chat.Id) ||
                (cl.RecievedMessage.TelegramMessageId == message.MessageId &&
                 cl.RecievedMessage.ChatId == message.Chat.Id)).Where(cl => cl.MotherChain.FinishedAt == null);
    }
}