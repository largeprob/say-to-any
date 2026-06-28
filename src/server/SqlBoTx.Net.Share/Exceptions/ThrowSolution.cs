using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace SqlBoTx.Net.Share.Exceptions
{
    public static class ThrowSolution
    {
        extension(object? obj)
        {
            public object ThrowIfNull()
            {
                if (obj == null)
                {
                    throw new ArgumentNullException("Argument cannot be null.");
                }
                return obj;
            }
        }

        extension(string? str)
        {
            public string ThrowIfNull()
            {
                if (str == null || string.IsNullOrWhiteSpace(str))
                {
                    throw new ArgumentNullException("Argument cannot be null.");
                }
                return str;
            }
        }
    }
}
