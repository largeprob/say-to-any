using System;
using System.Collections.Generic;
using System.Text;

namespace SqlBoTx.Net.Share.Exceptions
{
    public class BusinessException : Exception
    {
        public override string Message => base.Message;

        private readonly string _businessCode;
        public string BusinessCode
        {
            get { return _businessCode; }
        }

        public BusinessException(string businessCode, string? message) : base(message)
        {
            _businessCode = businessCode;
        }


    }
}
