using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.Exceptions
{
    public class DatabaseException : Exception
    {
        public DatabaseException() : base() { }

        public DatabaseException(string message) : base(message) { }

        public DatabaseException(string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }
}
