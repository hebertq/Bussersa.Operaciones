using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Modelo.ClasesGenericas
{
    public class CoreException : Exception
    {
        public CoreException()
            : base()
        {
        }

        public CoreException(String message)
            : base(message)
        {
        }

        public CoreException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public CoreException(String message, ExceptionContext ex)
           : base(message, ex.Exception)
        {
        }
        public CoreException(String message, HttpRequestException ex)
           : base(message, ex)
        {
        }
        public CoreException(String message, KeyNotFoundException ex)
           : base(message, ex)
        {
        }
    }
}
