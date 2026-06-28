using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlBoTx.Net.Domain.Organization;

namespace SqlBoTx.Net.EFCore.Mappings
{
    public class OrganizationRoleConfig : IEntityTypeConfiguration<OrganizationRole>
    {
        public void Configure(EntityTypeBuilder<OrganizationRole> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.Name).IsRequired();
        }
    }
}
