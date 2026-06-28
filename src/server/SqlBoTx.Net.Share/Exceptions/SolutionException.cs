using System;
using System.Collections.Generic;
using System.Text;

namespace SqlBoTx.Net.Share.Exceptions
{
    public class SolutionException : Exception
    {
        public override string Message => base.Message;

        public SolutionException(string? message) : base(message)
        {

        }
    }
}
