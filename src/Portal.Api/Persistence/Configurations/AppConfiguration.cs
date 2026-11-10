using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Api.Domain;

namespace Portal.Api.Persistence.Configurations;

public class AppConfiguration : IEntityTypeConfiguration<App>
{
    public void Configure(EntityTypeBuilder<App> builder)
    {
        builder.ToTable("apps");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.CustomerId)
            .HasMaxLength(App.CustomerIdMaxLength)
            .IsRequired();

        builder.Property(a => a.Name)
            .HasMaxLength(App.NameMaxLength)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Url)
            .HasMaxLength(255);

        builder.Property(a => a.InternalAdress)
            .HasMaxLength(255);

        builder.Property(a => a.FailureReason)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasIndex(a => new { a.CustomerId, a.Name })
            .IsUnique();
    }
}