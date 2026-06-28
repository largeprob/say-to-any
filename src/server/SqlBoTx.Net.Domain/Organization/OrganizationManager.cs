using SqlBoTx.Net.Share.Exceptions;

namespace SqlBoTx.Net.Domain.Organization
{
    public class OrganizationManager
    {
        private readonly IOrganizationRepository _organizationRepository;

        public OrganizationManager(IOrganizationRepository organizationRepository)
        {
            _organizationRepository = organizationRepository;
        }

        public async Task<Organization> CreateAsync(Organization input)
        {
            var existing = await _organizationRepository.ListAsync(q =>
                q.Where(x => x.Name == input.Name && x.ParentId == input.ParentId));
            if (existing.Count > 0)
            {
                throw new BusinessException("Organization001", $"同级下已存在名为 {input.Name} 的组织");
            }

            return input;
        }

        public async Task<Organization> UpdateAsync(Organization input)
        {
            var entity = await _organizationRepository.GetByIdAsync(input.Id);
            if (entity == null)
            {
                throw new BusinessException("Organization002", "组织不存在");
            }

            var existing = await _organizationRepository.ListAsync(q =>
                q.Where(x => x.Name == input.Name && x.ParentId == input.ParentId && x.Id != input.Id));
            if (existing.Count > 0)
            {
                throw new BusinessException("Organization003", $"同级下已存在名为 {input.Name} 的组织");
            }

            return input;
        }
    }
}
