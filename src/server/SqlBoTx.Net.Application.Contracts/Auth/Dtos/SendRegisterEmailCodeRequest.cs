using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SqlBoTx.Net.Application.Contracts.Auth.Dtos
{
    public class SendRegisterEmailCodeRequest
    {
        [DisplayName("邮箱账号")]
        [Required(ErrorMessage = "{0}不能为空")]
        [EmailAddress(ErrorMessage = "{0}格式不正确")]
        public string? Email { get; set; }
    }
}
