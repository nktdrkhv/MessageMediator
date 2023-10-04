using MessageMediator.ProofOfConcept.Aggregates;
using MessageMediator.ProofOfConcept.Configuration;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramUpdater.UpdateContainer;
using TelegramUpdater.UpdateHandlers.Scoped;
using TelegramUpdater.UpdateHandlers.Scoped.ReadyToUse;

namespace MessageMediator.ProofOfConcept.UpdateHandlers.Messages;

[Order(102)]
public sealed class RegularIssue : MessageHandler
{
    private readonly BotDbContext _context;
    private readonly BotConfiguration _conf;

    public RegularIssue(BotDbContext context, IOptions<BotConfiguration> conf)
    {
        _context = context;
        _conf = conf.Value;
    }

    protected override async Task HandleAsync(IContainer<Message> cntr)
    {
        // -----------------------------------
        // ----------- get entities ----------
        // -----------------------------------

        var text = cntr.Update.Text ?? cntr.Update.Caption;
        if (text == null)
            return;

        var entities = cntr.Update.Entities?.Zip(cntr.Update.EntityValues!,
            (first, second) => new OriginalEntity
            {
                Text = second[1..],
                Type = first.Type,
                Offset = first.Offset + first.Length
            });
        if (entities == null)
            return;

        // -----------------------------------
        // ---------- match entities ---------
        // -----------------------------------

        if (await _context.LocalChats.Where(lc => lc.Id == cntr.Update.Chat.Id)
            .AsSplitQuery()
            .Include(lc => lc.DecisionMakers)
            .Include(lc => lc.SourcingFor)
            .SingleOrDefaultAsync() is LocalChat chat)
        {
            var matchesQuery = from s in chat.SourcingFor!.AsQueryable()
                               join t in _context.Triggers on s.Id equals t.SourceId
                               join e in entities!.AsQueryable() on t.Type equals e.Type
                               where t.Text == e.Text
                               where !s.IsDisabled && !s.IsDeleted && !t.IsDisabled
                               select new MatchedTrigger
                               {
                                   TriggerId = t.Id,
                                   Behaviour = t.Behaviour,
                                   Offset = e.Offset
                               };
            var matches = await matchesQuery.ToArrayAsync();
            if (!matches.Any())
                return;
            for (var i = 0; i < matches.Length; i++)
            {
                if (i - 1 >= 0)
                    matches[i].PreviousOffset = matches[i - 1].Offset;
                if (i < matches.Length - 1)
                    matches[i].NextOffset = matches[i + 1].Offset;
            }

            // -----------------------------------
            // ---------- check rights -----------
            // -----------------------------------

            // todo: check fake user in non-channel chats
            var user = cntr.Update.From != null ? await _context.GetOrCreateLocalUserAsync(cntr.Update.From!) : null;
            if ((chat.DecisionMakers != null && user == null) ||
                (chat.DecisionMakers != null && user != null && !chat.DecisionMakers.Contains(user)))
            {
                await cntr.ResponseAsync("Вы не находитесь в списке разрешённых отправителей");
                return;
            }

            // -----------------------------------
            // ---------- preparing data ---------
            // -----------------------------------

            LocalMessage reason;
            if (cntr.Update.MediaGroupId is string mediaGroup)
            {
                List<Message> messageGroup = new() { cntr.Update };
                while (await AwaitMessageAsync(filter: null, timeOut: TimeSpan.FromSeconds(1)) is IContainer<Message> msgCntr && mediaGroup.Equals(msgCntr.Update.MediaGroupId))
                    messageGroup.Add(msgCntr.Update);
                reason = new LocalMessage(text, messageGroup.ToArray());
            }
            else
                reason = new LocalMessage(text, cntr.Update);

            // -----------------------------------
            // ----------- broadcasting ----------
            // -----------------------------------

            using var transaction = _context.Database.BeginTransaction();

            foreach (var match in matches)
            {
                var trigger = _context.Triggers
                    .Where(t => t.Id == match.TriggerId)
                    .AsSplitQuery()
                    .Include(t => t.Workers)
                    .Include(t => t.Supervisors)
                    .Single();
                var worker = trigger.Workers.First(w => !w.IsDisabled && !w.IsDeleted);
                var supervisor = trigger.Supervisors!.FirstOrDefault(s => !s.IsDisabled && !s.IsDeleted);

                var concreteData = new MessageData(reason.Data)
                {
                    Text = match.Behaviour switch
                    {
                        TriggerBehaviour.Full => text.Trim(),
                        TriggerBehaviour.Before => text[..match.Offset].Trim(),
                        TriggerBehaviour.Between => text[match.PreviousOffset..match.NextOffset].Trim(),
                        TriggerBehaviour.After => text[match.Offset..].Trim(),
                        _ => throw new NotImplementedException()
                    }
                };

                var chain = new Chain()
                {
                    Trigger = trigger,
                    Worker = worker!,
                    Supervisor = supervisor,
                    Reason = reason,
                    PreparedData = concreteData
                };
                await _context.Chains.AddAsync(chain);
                await _context.SaveChangesAsync();

                var forwarded = await cntr.BotClient.SendIssueAsync(chain);
                var chainLink = new ChainLink()
                {
                    MotherChain = chain,
                    RecievedMessage = reason,
                    ForwardMessage = forwarded
                };
                await _context.ChainLinks.AddAsync(chainLink);
                await _context.SaveChangesAsync();

                transaction.Commit();

                // -----------------------------------
                // ---------- admins notify ----------
                // -----------------------------------

                foreach (var adminId in _conf.Administrators)
                    await cntr.BotClient.SendTextMessageAsync(adminId,
                        text: $"Задача <b>{chainLink.MotherChain.Trigger.Label}</b>\nотправлена из <i>{chainLink.RecievedMessage.Chat.DefaultAlias ?? chainLink.RecievedMessage.Chat.Name}</i>\nисполнителю <i>{chainLink.ForwardMessage.Chat.DefaultAlias ?? chainLink.ForwardMessage.Chat.Name}</i>",
                        disableNotification: true,
                        parseMode: ParseMode.Html);
            }
        }
    }
}