using System.Reflection;
using MessageMediator.ProofOfConcept.Entities;
using Microsoft.EntityFrameworkCore;

namespace MessageMediator.ProofOfConcept.Persistance;

public class BotDbContext : DbContext
{


    public BotDbContext()
    {
        Database.EnsureDeleted();
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=oeztomsk.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<MessageData>()
            .HasMany(md => md.MediaFiles).WithMany();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }
}