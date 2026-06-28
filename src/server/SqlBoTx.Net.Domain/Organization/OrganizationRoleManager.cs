using SqlBoTx.Net.Share.Exceptions;

namespace SqlBoTx.Net.Domain.Organization
{
    public class OrganizationRoleManager
    {
        private readonly IOrganizationRoleRepository _organizationRoleRepository;

        public OrganizationRoleManager(IOrganizationRoleRepository organizationRoleRepository)
        {
            _organizationRoleRepository = organizationRoleRepository;
        }

        public async Task<OrganizationRole> CreateAsync(OrganizationRole input)
        {
            var existing = await _organizationRoleRepository.ListAsync(q =>
                q.Where(x => x.Name == input.Name));
            if (existing.Count > 0)
            {
                throw new BusinessException("OrganizationRole001", $"角色名称 {input.Name} 已存在");
            }

            input.CreatedDate = DateTime.Now;
            return input;
        }

        public async Task<OrganizationRole> UpdateAsync(OrganizationRole input)
        {
            var entity = await _organizationRoleRepository.GetByIdAsync(input.Id);
            if (entity == null)
            {
                throw new BusinessException("OrganizationRole002", "角色不存在");
            }

            var existing = await _organizationRoleRepository.ListAsync(q =>
                q.Where(x => x.Name == input.Name && x.Id != input.Id));
            if (existing.Count > 0)
            {
                throw new BusinessException("OrganizationRole003", $"角色名称 {input.Name} 已存在");
            }

            return input;
        }
    }
}
