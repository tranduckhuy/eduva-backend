using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Eduva.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.JobStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(j => j.SourceBlobNames)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>())
            .HasMaxLength(255)
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(j => j.Topic)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(j => j.ContentBlobName)
            .HasMaxLength(500);

        builder.Property(j => j.VideoOutputBlobName)
            .HasMaxLength(500);

        builder.Property(j => j.AudioOutputBlobName)
            .HasMaxLength(500);

        builder.Property(j => j.FailureReason)
            .HasMaxLength(1000);

        builder.Property(j => j.UserId)
            .IsRequired();

        builder.HasIndex(j => j.JobStatus);
        builder.HasIndex(j => j.UserId);

        builder.HasOne(j => j.User)
            .WithMany()
            .HasForeignKey(j => j.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
