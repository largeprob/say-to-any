namespace SqlBoTx.Net.Domain.Organization
{
    public interface IOrganizationUserRepository
    {
        IQueryable<OrganizationUser> IQueryable();

        Task<OrganizationUser?> GetByIdAsync(long id);

        Task<List<OrganizationUser>> ListAsync(Func<IQueryable<OrganizationUser>, IQueryable<OrganizationUser>>? includeFunc = null);

        Task InsterAsync(OrganizationUser entity);

        Task UpdateAsync(OrganizationUser entity);

        Task DeleteAsync(long id);
    }
}
