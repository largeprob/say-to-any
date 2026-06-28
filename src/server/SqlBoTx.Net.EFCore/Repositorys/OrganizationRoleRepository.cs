using Microsoft.EntityFrameworkCore;
using SqlBoTx.Net.Domain.Organization;

namespace SqlBoTx.Net.EFCore.Repositorys
{
    public class OrganizationRoleRepository : IOrganizationRoleRepository
    {
        private readonly SqlBotxDBContext _dbContext;

        public OrganizationRoleRepository(SqlBotxDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<OrganizationRole> IQueryable()
        {
            return _dbContext.Set<OrganizationRole>();
        }

        public async Task<OrganizationRole?> GetByIdAsync(long id)
        {
            return await _dbContext.Set<OrganizationRole>()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<OrganizationRole>> ListAsync(Func<IQueryable<OrganizationRole>, IQueryable<OrganizationRole>>? includeFunc = null)
        {
            var query = _dbContext.Set<OrganizationRole>().AsQueryable();
            if (includeFunc != null)
            {
                query = includeFunc(query);
            }
            return await query.ToListAsync();
        }

        public async Task InsterAsync(OrganizationRole entity)
        {
            await _dbContext.Set<OrganizationRole>().AddAsync(entity);
        }

        public async Task UpdateAsync(OrganizationRole entity)
        {
            var existingEntity = await _dbContext.Set<OrganizationRole>()
                .FirstAsync(x => x.Id == entity.Id);
            _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
        }

        public async Task DeleteAsync(long id)
        {
            await _dbContext.Set<OrganizationRole>()
               .Where(x => x.Id == id)
               .ExecuteDeleteAsync();
        }
    }
}
