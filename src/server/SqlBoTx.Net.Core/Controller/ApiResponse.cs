using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SqlBoTx.Net.Core.Controller
{
    public class ApiResponse<T>
    {
        /// <summary>
        /// 状态码
        /// </summary>
        [Description("状态码")]
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; }
        public List<ValidationError>? Errors { get; set; }

        public static ApiResponse<T> Fail(int code, string message, List<ValidationError>? errors = null)
        {
            return new ApiResponse<T>
            {
                Code = code,
                Message = message,
                Errors = errors
            };
        }

        public static ApiResponse<T> Success(int code, string message, T data)
        {
            return new ApiResponse<T>
            {
                Code = code,
                Message = message,
                Data = data
            };
        }
    }
    public record ValidationError(string Field, string Error);
}
