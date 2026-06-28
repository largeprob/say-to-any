using Microsoft.EntityFrameworkCore;
using SqlBoTx.Net.Domain.Organization;

namespace SqlBoTx.Net.EFCore.Repositorys
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly SqlBotxDBContext _dbContext;

        public OrganizationRepository(SqlBotxDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<Organization> IQueryable()
        {
            return _dbContext.Set<Organization>();
        }

        public async Task<Organization?> GetByIdAsync(long id)
        {
            return await _dbContext.Set<Organization>()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Organization>> ListAsync(Func<IQueryable<Organization>, IQueryable<Organization>>? includeFunc = null)
        {
            var query = _dbContext.Set<Organization>().AsQueryable();
            if (includeFunc != null)
            {
                query = includeFunc(query);
            }
            return await query.ToListAsync();
        }

        public async Task InsterAsync(Organization entity)
        {
            await _dbContext.Set<Organization>().AddAsync(entity);
        }

        public async Task UpdateAsync(Organization entity)
        {
            var existingEntity = await _dbContext.Set<Organization>()
                .FirstAsync(x => x.Id == entity.Id);
            _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
        }

        public async Task DeleteAsync(long id)
        {
            await _dbContext.Set<Organization>()
               .Where(x => x.Id == id)
               .ExecuteDeleteAsync();
        }
    }
}
