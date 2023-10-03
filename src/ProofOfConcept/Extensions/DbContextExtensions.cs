using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Persistance;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Extensions;

public static class DbContextExtensions
{
    public static async ValueTask<LocalUser> GetOrCreateLocalUserAsync(this BotDbContext context, User user)
    {
        if (await context.LocalUsers.FindAsync(user.Id) is LocalUser localUser)
            return localUser;
        else
        {
            var newUser = new LocalUser(user);
            await context.LocalUsers.AddAsync(newUser);
            await context.SaveChangesAsync();
            return newUser;
        }
    }
}