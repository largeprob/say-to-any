using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlBoTx.Net.Domain.Organization;

namespace SqlBoTx.Net.EFCore.Mappings
{
    public class OrganizationUserConfig : IEntityTypeConfiguration<OrganizationUser>
    {
        public void Configure(EntityTypeBuilder<OrganizationUser> builder)
        {
            // 主键
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            // 属性配置
            builder.Property(x => x.UserName).IsRequired();
            builder.Property(x => x.LoginAccount).IsRequired();
            builder.Property(x => x.Password).IsRequired();
            builder.Property(x => x.OrganizationId).IsRequired();
            builder.Property(x => x.OrganizationRoleId).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.LastLoginDate);

            builder.HasOne(x => x.Organization)
                   .WithMany(x => x.Users)
                   .HasForeignKey(x => x.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.OrganizationRole)
                   .WithMany(x => x.Users)
                   .HasForeignKey(x => x.OrganizationRoleId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
