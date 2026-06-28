using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SqlBoTx.Net.Share.Helpers
{
    /// <summary>
    /// 字符串帮助类
    /// </summary>
    public class StringHelper
    {

        /// <summary>
        /// 将驼峰式命名转为下划线命名
        /// </summary>
        public static string ConvertToSnakeCase(string input)
        {
            return Regex.Replace(input, @"([a-z])([A-Z])", "$1_$2").ToLower();
        }
    }
}
