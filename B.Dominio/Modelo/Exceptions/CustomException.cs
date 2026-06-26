using System;
using System.Globalization;

namespace Modelo.Exceptions
{
    public class CustomException : Exception
    {
        public CustomException() : base() { }
        public CustomException(string message) : base(message) { }
        public CustomException(string message, params object[] args)
            : base(string.Format(CultureInfo.CurrentCulture, message, args)) { }
        public CustomException(string message, Exception inner,params object[] args)
            : base(string.Format(CultureInfo.CurrentCulture, message, inner,args)) { }
    }
}
