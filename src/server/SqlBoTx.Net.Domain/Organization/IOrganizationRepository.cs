namespace SqlBoTx.Net.Domain.Organization
{
    public interface IOrganizationRepository
    {
        IQueryable<Organization> IQueryable();

        Task<Organization?> GetByIdAsync(long id);

        Task<List<Organization>> ListAsync(Func<IQueryable<Organization>, IQueryable<Organization>>? includeFunc = null);

        Task InsterAsync(Organization entity);

        Task UpdateAsync(Organization entity);

        Task DeleteAsync(long id);
    }
}
