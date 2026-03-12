using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayGateway.Domain.Entities;

namespace PayGateway.Infrastructure.Persistence.Configurations;

internal class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.IdempotencyKey)
            .IsUnique();

        builder.Property(p => p.MerchantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 4);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.PixKey)
            .HasMaxLength(200);

        builder.Property(p => p.PixKeyType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.CardLast4)
            .HasMaxLength(4);

        builder.Property(p => p.CardBrand)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();
    }
}
