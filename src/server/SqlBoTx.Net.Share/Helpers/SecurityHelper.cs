using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SqlBoTx.Net.Share.Helpers
{
    public class SecurityHelper
    {
        /// <summary>
        /// 生成盐值
        /// </summary>
        /// <returns></returns>
        public static string GenerateSalt()
        {
            var saltBytes = new byte[16];
            RandomNumberGenerator.Fill(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// 加密密码
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static string HashStr(string str, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var salted = string.Format("{0}{1}", str, salt);
                var saltedBytes = Encoding.UTF8.GetBytes(salted);
                var hashBytes = sha256.ComputeHash(saltedBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
