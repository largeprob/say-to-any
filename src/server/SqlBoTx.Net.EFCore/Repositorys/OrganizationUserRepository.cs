using Microsoft.EntityFrameworkCore;
using SqlBoTx.Net.Domain.Organization;

namespace SqlBoTx.Net.EFCore.Repositorys
{
    public class OrganizationUserRepository : IOrganizationUserRepository
    {
        private readonly SqlBotxDBContext _dbContext;

        public OrganizationUserRepository(SqlBotxDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<OrganizationUser> IQueryable()
        {
            return _dbContext.Set<OrganizationUser>();
        }

        public async Task<OrganizationUser?> GetByIdAsync(long id)
        {
            return await _dbContext.Set<OrganizationUser>()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<OrganizationUser>> ListAsync(Func<IQueryable<OrganizationUser>, IQueryable<OrganizationUser>>? includeFunc = null)
        {
            var query = _dbContext.Set<OrganizationUser>().AsQueryable();
            if (includeFunc != null)
            {
                query = includeFunc(query);
            }
            return await query.ToListAsync();
        }

        public async Task InsterAsync(OrganizationUser entity)
        {
            await _dbContext.Set<OrganizationUser>().AddAsync(entity);
        }

        public async Task UpdateAsync(OrganizationUser entity)
        {
            var existingEntity = await _dbContext.Set<OrganizationUser>()
                .FirstAsync(x => x.Id == entity.Id);
            _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
        }

        public async Task DeleteAsync(long id)
        {
            await _dbContext.Set<OrganizationUser>()
               .Where(x => x.Id == id)
               .ExecuteDeleteAsync();
        }
    }
}
