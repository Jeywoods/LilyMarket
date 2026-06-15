using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LilyMarket.Infrastructure.Data.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Category)
            .HasMaxLength(50);

        builder.Property(x => x.Condition)
            .HasMaxLength(20);

        builder.Property(x => x.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.StartingPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MinimumIncrement)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.BuyNowPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CurrentHighestBid)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Status)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AuctionStatus>(v))
            .HasMaxLength(20);

        builder.HasIndex(x => new { x.Status, x.EndTime });

        builder.HasMany(x => x.Bids)
            .WithOne()
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}