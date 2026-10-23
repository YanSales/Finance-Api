using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        // O Id e gerado pelo dominio (GUID v7): o banco nao gera valor.
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(User.EmailMaxLength);

        // Garantia real de unicidade do e-mail (inclusive em cadastros concorrentes).
        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("UX_Users_Email");

        // Hash V3 do Identity tem ~124 caracteres; folga para evolucao do algoritmo.
        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(user => user.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();
    }
}
