using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SqlBoTx.Net.Application.Contracts.Auth.Dtos
{
    public class LoginRequest
    {
        [DisplayName("登录账号")]
        [Required(ErrorMessage = "{0}不能为空")]
        public string? LoginAccount { get; set; }

        [DisplayName("登录密码")]
        [Required(ErrorMessage = "{0}不能为空")]
        public string? Password { get; set; }
    }
}
