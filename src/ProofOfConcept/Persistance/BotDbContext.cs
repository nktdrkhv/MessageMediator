using System.Reflection;
using MessageMediator.ProofOfConcept.Abstract;
using MessageMediator.ProofOfConcept.Entities;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace MessageMediator.ProofOfConcept.Persistance;

public class BotDbContext : DbContext
{
    public DbSet<Invitation> Invitations { get; set; } = null!;
    public DbSet<Chain> Chains { get; set; } = null!;
    public DbSet<ChainLink> ChainLinks { get; set; } = null!;
    public DbSet<Trigger> Triggers { get; set; } = null!;
    public DbSet<LocalChat> LocalChats { get; set; } = null!;
    public DbSet<LocalUser> LocalUsers { get; set; } = null!;
    public DbSet<Source> Sources { get; set; } = null!;
    public DbSet<Worker> Workers { get; set; } = null!;
    public DbSet<Supervisor> Supervisors { get; set; } = null!;
    public DbSet<MessageData> MessageData { get; set; } = null!;
    public DbSet<Media> Media { get; set; } = null!;
    public DbSet<Contact> Contacts { get; set; } = null!;

    public BotDbContext()
    {
        // Database.EnsureDeleted();
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("Data Source=mm.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<LocalMessage>()
            .Navigation(lm => lm.Data).AutoInclude();

        modelBuilder.Entity<MessageData>()
            .Navigation(md => md.Media).AutoInclude();
        modelBuilder.Entity<MessageData>()
            .Navigation(md => md.Contact).AutoInclude();

        modelBuilder.Entity<Contact>()
            .Property(typeof(int), "Id")
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<Contact>()
            .HasKey("Id");

        modelBuilder.Entity<TelegramEntity>()
            .UseTpcMappingStrategy();
        modelBuilder.Entity<TelegramEntity>()
            .Property(te => te.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<LocalChat>()
            .HasMany(lc => lc.DecisionMakers)
                .WithMany(lu => lu.ResponsibleFor)
                .UsingEntity(join => join.ToTable("ChatRuler"));
        modelBuilder.Entity<LocalChat>()
            .HasMany(lc => lc.SourcingFor)
                .WithMany(s => s.Submitters)
                .UsingEntity(join => join.ToTable("Submitter"));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }
}