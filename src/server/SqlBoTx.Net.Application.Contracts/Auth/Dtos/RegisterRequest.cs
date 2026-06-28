using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SqlBoTx.Net.Application.Contracts.Auth.Dtos
{
    public class RegisterRequest
    {
        [DisplayName("邮箱账号")]
        [Required(ErrorMessage = "{0}不能为空")]
        [EmailAddress(ErrorMessage = "{0}格式不正确")]
        public string? Email { get; set; }

        [DisplayName("邮箱验证码")]
        [Required(ErrorMessage = "{0}不能为空")]
        public string? VerificationCode { get; set; }

        [DisplayName("登录密码")]
        [Required(ErrorMessage = "{0}不能为空")]
        [MinLength(8, ErrorMessage = "{0}长度至少为8位")]
        public string? Password { get; set; }
    }
}
