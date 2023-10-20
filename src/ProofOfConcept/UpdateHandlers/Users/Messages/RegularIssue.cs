using MessageMediator.ProofOfConcept.Configuration;
using MessageMediator.ProofOfConcept.Dto;
using MessageMediator.ProofOfConcept.Entities;
using MessageMediator.ProofOfConcept.Enums;
using MessageMediator.ProofOfConcept.Extensions;
using MessageMediator.ProofOfConcept.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    private readonly BotConfiguration _conf;
    private readonly BotDbContext _context;

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

        string? text = cntr.Update.Text ?? cntr.Update.Caption;
        if (text == null)
        {
            return;
        }

        MessageEntity[]? msgEntities = cntr.Update.Entities ?? cntr.Update.CaptionEntities;
        IEnumerable<string>? msgEntityValues = cntr.Update.EntityValues ?? cntr.Update.CaptionEntityValues;
        IEnumerable<OriginalEntity>? entities = msgEntities?.Zip(msgEntityValues!,
            (first, second) => new OriginalEntity
            {
                // warning: only for hashtags #, mentions @ etc.
                Text = second[1..], Type = first.Type, Offset = first.Offset, Length = first.Length
            });
        if (entities == null)
        {
            return;
        }

        // -----------------------------------
        // ---------- match entities ---------
        // -----------------------------------

        if (await _context.LocalChats.Where(lc => lc.Id == cntr.Update.Chat.Id)
                .AsSplitQuery()
                .Include(lc => lc.DecisionMakers)
                .Include(lc => lc.SourcingFor)
                .SingleOrDefaultAsync() is LocalChat chat)
        {
            IQueryable<MatchedTrigger> matchesQuery =
                from s in chat.SourcingFor!.AsQueryable()
                join t in _context.Triggers on s.Id equals t.SourceId
                join e in entities.AsQueryable() on t.Type equals e.Type
                where t.Text == e.Text
                where !s.IsDisabled && !s.IsDeleted && !t.IsDisabled
                select new MatchedTrigger
                {
                    TriggerId = t.Id, Behaviour = t.Behaviour, Offset = e.Offset, Length = e.Length
                };
            MatchedTrigger[] matches = await matchesQuery.ToArrayAsync();
            if (matches.Length == 0)
            {
                return;
            }

            for (int i = 0; i < matches.Length; i++)
            {
                if (i - 1 >= 0)
                {
                    matches[i].Previous = (matches[i - 1].Offset, matches[i - 1].Length);
                }

                if (i < matches.Length - 1)
                {
                    matches[i].Next = (matches[i + 1].Offset, matches[i + 1].Length);
                }
            }

            // -----------------------------------
            // ---------- check rights -----------
            // -----------------------------------

            // todo: check fake user in non-channel chats
            LocalUser? user = cntr.Update.From != null
                ? await _context.GetOrCreateLocalUserAsync(cntr.Update.From!)
                : null;
            if ((chat.DecisionMakers != null && user == null) ||
                (chat.DecisionMakers != null && user != null && !chat.DecisionMakers.Contains(user)))
            {
                await cntr.ResponseAsync("Вы не находитесь в списке разрешённых отправителей");
                return;
            }

            // -----------------------------------
            // ---------- preparing data ---------
            // -----------------------------------

            ICollection<LocalMessage> origialRecieved;
            if (cntr.Update.MediaGroupId is string mediaGroup)
            {
                List<Message> messageGroup = new() { cntr.Update };
                while (await AwaitMessageAsync(
                           null,
                           TimeSpan.FromSeconds(1)) is IContainer<Message> msgCntr &&
                       mediaGroup.Equals(msgCntr.Update.MediaGroupId))
                {
                    messageGroup.Add(msgCntr.Update);
                }

                origialRecieved = LocalMessage.FromSet(text, messageGroup.OrderBy(m => m.MessageId).ToArray());
            }
            else
            {
                origialRecieved = new LocalMessage[] { new(text, cntr.Update) };
            }

            // -----------------------------------
            // ----------- broadcasting ----------
            // -----------------------------------

            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

            foreach (MatchedTrigger? match in matches)
            {
                Trigger trigger = _context.Triggers
                    .Where(t => t.Id == match.TriggerId)
                    .AsSplitQuery()
                    .Include(t => t.Workers)
                    .Include(t => t.Supervisors)
                    .Single();
                Worker worker = trigger.Workers.First(w => w is { IsDisabled: false, IsDeleted: false });
                Supervisor? supervisor =
                    trigger.Supervisors!.FirstOrDefault(s => s is { IsDisabled: false, IsDeleted: false });

                // -------- poining the data ---------
                List<MessageData> concreteData =
                    new List<MessageData> { ExtractText(origialRecieved.First().Data, text, match) };
                foreach (LocalMessage reason in origialRecieved.Skip(1))
                {
                    concreteData.Add(reason.Data);
                }

                Chain chain = new Chain
                {
                    Trigger = trigger,
                    SourceChat = chat,
                    Worker = worker,
                    Supervisor = supervisor,
                    PreparedData = concreteData
                };
                await _context.Chains.AddAsync(chain);
                await _context.SaveChangesAsync();

                if (supervisor != null && worker.IsOnProbation)
                {
                    ICollection<LocalMessage> sentToSupervisor = await cntr.BotClient.SendIssueToSupervisorAsync(chain);
                    await _context.ChainLinks.AddRangeAsync(
                        origialRecieved.Zip(sentToSupervisor).Select(
                            (pair, _) => new ChainLink(chain, pair.First, pair.Second)));

                    ICollection<LocalMessage> sentToWorker = await cntr.BotClient.SendIssueToWorkerAsync(chain);
                    await _context.ChainLinks.AddRangeAsync(
                        sentToSupervisor.Zip(sentToWorker).Select(
                            (pair, _) => new ChainLink(chain, pair.First, pair.Second)));
                }
                else
                {
                    ICollection<LocalMessage> sentToWorker = await cntr.BotClient.SendIssueToWorkerAsync(chain);
                    await _context.ChainLinks.AddRangeAsync(
                        origialRecieved.Zip(sentToWorker).Select(
                            (pair, _) => new ChainLink(chain, pair.First, pair.Second)));
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // -----------------------------------
                // ---------- admins notify ----------
                // -----------------------------------

                string? triggerLabel = chain.Trigger.Label;
                string sourceLabel = chat.Alias ?? chat.Name;
                string workerLabel = worker.Chat.Alias ?? worker.Chat.Name;

                foreach (long adminId in _conf.Administrators)
                {
                    await cntr.BotClient.SendTextMessageAsync(adminId,
                        $"Задача <b>{triggerLabel}</b>\nотправлена из <i>{sourceLabel}</i>\nисполнителю <i>{workerLabel}</i>",
                        disableNotification: true,
                        parseMode: ParseMode.Html);
                }
            }
        }
    }

    public static MessageData ExtractText(MessageData data, string text, MatchedTrigger match)
    {
        return new MessageData(data)
        {
            // todo: check array boundaries
            Text = match.Behaviour switch
            {
                TriggerBehaviour.Full => text.Trim(),
                TriggerBehaviour.Before => text[..match.Offset].Trim(),
                TriggerBehaviour.Between => text[(match.Offset + match.Length)..match.Next.Offset].Trim(),
                TriggerBehaviour.After => text[(match.Offset + match.Length)..].Trim(),
                _ => throw new NotImplementedException()
            }
        };
    }
}