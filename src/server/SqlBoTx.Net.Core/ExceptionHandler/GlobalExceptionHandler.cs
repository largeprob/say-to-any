using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using SqlBoTx.Net.Share.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlBoTx.Net.Core.ExceptionHandler
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService problemDetailsService;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService)
        {
            this.problemDetailsService = problemDetailsService;
        }

        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            //先判断是否是业务异常
            if (exception is BusinessException)
            {
                var ProblemContext = new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    Exception = exception,
                    ProblemDetails =
                    {
                        Title = "BusinessError",
                        Detail = exception.Message,
                        Status = StatusCodes.Status400BadRequest,
                    },
                };
                return problemDetailsService.TryWriteAsync(ProblemContext);
            }

            //处理其他异常
            if (exception is not ApplicationException)
                return ValueTask.FromResult(false);

            //处理应用异常
            var problemDetailsContext = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails =
                {
                    Title = "ApplicationError",
                    Detail = exception.Message,
                    Status = StatusCodes.Status500InternalServerError,
                },
            };
            return problemDetailsService.TryWriteAsync(problemDetailsContext);
        }
    }
}
