namespace SqlBoTx.Net.Domain.Organization
{
    public interface IOrganizationRoleRepository
    {
        IQueryable<OrganizationRole> IQueryable();

        Task<OrganizationRole?> GetByIdAsync(long id);

        Task<List<OrganizationRole>> ListAsync(Func<IQueryable<OrganizationRole>, IQueryable<OrganizationRole>>? includeFunc = null);

        Task InsterAsync(OrganizationRole entity);

        Task UpdateAsync(OrganizationRole entity);

        Task DeleteAsync(long id);
    }
}
