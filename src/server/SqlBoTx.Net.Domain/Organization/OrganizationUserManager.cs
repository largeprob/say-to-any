using SqlBoTx.Net.Share.Exceptions;
using SqlBoTx.Net.Share.Helpers;

namespace SqlBoTx.Net.Domain.Organization
{
    public class OrganizationUserManager
    {
        private readonly IOrganizationUserRepository _organizationUserRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationRoleRepository _organizationRoleRepository;

        public OrganizationUserManager(
            IOrganizationUserRepository organizationUserRepository,
            IOrganizationRepository organizationRepository,
            IOrganizationRoleRepository organizationRoleRepository)
        {
            _organizationUserRepository = organizationUserRepository;
            _organizationRepository = organizationRepository;
            _organizationRoleRepository = organizationRoleRepository;
        }

        public async Task<OrganizationUser> CreateAsync(OrganizationUser input)
        {
            var org = await _organizationRepository.GetByIdAsync(input.OrganizationId);
            if (org == null)
            {
                throw new BusinessException("OrganizationUser001", "所属组织不存在");
            }

            var role = await _organizationRoleRepository.GetByIdAsync(input.OrganizationRoleId);
            if (role == null)
            {
                throw new BusinessException("OrganizationUser002", "所属角色不存在");
            }

            var email = input.Email?.Trim();
            var existing = await _organizationUserRepository.ListAsync(q =>
                q.Where(x =>
                    x.LoginAccount == input.LoginAccount
                    || (!string.IsNullOrEmpty(email) && x.Email == email)));
            if (existing.Count > 0)
            {
                throw new BusinessException("OrganizationUser003", $"登录账号或邮箱 {input.LoginAccount} 已存在");
            }

            // 密码加密存储
            var salt = SecurityHelper.GenerateSalt();
            input.Password = SecurityHelper.HashStr(input.Password, salt) + "|" + salt;

            input.CreatedDate = DateTime.Now;
            return input;
        }

        public async Task<OrganizationUser> UpdateAsync(OrganizationUser input)
        {
            var entity = await _organizationUserRepository.GetByIdAsync(input.Id);
            if (entity == null)
            {
                throw new BusinessException("OrganizationUser004", "用户不存在");
            }

            var role = await _organizationRoleRepository.GetByIdAsync(input.OrganizationRoleId);
            if (role == null)
            {
                throw new BusinessException("OrganizationUser005", "所属角色不存在");
            }

            var email = input.Email?.Trim();
            var existing = await _organizationUserRepository.ListAsync(q =>
                q.Where(x =>
                    x.Id != input.Id
                    && (x.LoginAccount == input.LoginAccount
                        || (!string.IsNullOrEmpty(email) && x.Email == email))));
            if (existing.Count > 0)
            {
                throw new BusinessException("OrganizationUser006", $"登录账号或邮箱 {input.LoginAccount} 已存在");
            }

            // 如果密码有变更，重新加密
            if (!string.IsNullOrEmpty(input.Password) && input.Password != entity.Password)
            {
                var salt = SecurityHelper.GenerateSalt();
                input.Password = SecurityHelper.HashStr(input.Password, salt) + "|" + salt;
            }
            if (string.IsNullOrEmpty(input.Password))
            {
                input.Password = entity.Password;
            }

            return input;
        }
    }
}
