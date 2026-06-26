using System;
using System.Collections.Generic;

namespace Modelo.Exceptions
{
    [Serializable]
    public class RulesException : Exception
    {
        private readonly List<ErrorInfo> _errors;

        public RulesException(string propertyName, string errorMessage, string prefix = "")
        {
            _errors = Errors;
            _errors.Add(new ErrorInfo($"{prefix}{propertyName}", errorMessage));
        }

        public RulesException(string propertyName, string errorMessage, object onObject, string prefix = "")
        {
            _errors = Errors;
            _errors.Add(new ErrorInfo($"{prefix}{propertyName}", errorMessage, onObject));
        }

        public RulesException()
        {
            _errors = Errors;
        }

        public RulesException(List<ErrorInfo> errors)
        {
            _errors = errors;
        }

        public List<ErrorInfo> Errors
        {
            get
            {
                return _errors ?? new List<ErrorInfo>();
            }
        }
    }

    public class ErrorInfo
    {
        private readonly string _errorMessage;
        private readonly string _propertyName;
        private readonly object _onObject;

        public ErrorInfo(string propertyName, string errorMessage)
        {
            _propertyName = propertyName;
            _errorMessage = errorMessage;
            _onObject = null;
        }

        public ErrorInfo(string propertyName, string errorMessage, object onObject)
        {
            _propertyName = propertyName;
            _errorMessage = errorMessage;
            _onObject = onObject;
        }

        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
        }

        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }
    }
}
