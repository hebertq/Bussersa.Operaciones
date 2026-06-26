using Modelo.ClasesGenericas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.Validaciones
{
    public abstract class BaseValidationRuleSet<T> where T : class
    {
        public List<ValidationRule<T>> RuleList { get; internal set; }
        public Object BusinessObject { set { _businessObject = value; } }
        private InformacionTransaccion _transaction { set; get; }

        private Boolean _validationStatus { get; set; }
        private Object _businessObject;


        /// <summary>
        /// Initiates rule list
        /// </summary>
        public BaseValidationRuleSet()
        {
            RuleList = new List<ValidationRule<T>>();
            _transaction = new InformacionTransaccion();
            _validationStatus = false;
            _businessObject = new object();
        }

        /// <summary>
        /// Run all added rules
        /// </summary>
        /// <returns></returns>
        public InformacionTransaccion Validar()
        {
            foreach (var rule in this.RuleList)
            {
                var result = rule.RunRuleDelegate();
                if (!result)
                {
                    AddValidationError(rule.PropertyName, rule.Message, true);
                }
            }
            return _transaction;
        }

        /// <summary>
        /// Agregar errores de validación
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="errorMessage"></param>
        private void AddValidationError(string propertyName, string errorMessage, bool valor = false)
        {
            bool property = _transaction.ValidationErrors.Contains(propertyName), valuekey = _transaction.ValidationErrors.ContainsValue(errorMessage);

            if (_transaction.ValidationErrors.Count == 0)
            {
                _transaction.ErrorCode = "-1";
                _transaction.TipoError = ErrorType.Validación;
                _transaction.IsError = true;
                _transaction.ReturnMessage = string.Format("Usuario: {0}, Tipo de Error: ", "Sistemas") + ErrorType.Validación + System.Environment.NewLine +
                                                    "                           Codigo: " + _transaction.ErrorCode + System.Environment.NewLine +
                                                    "                           Mensaje: ";
            }

            if (property == false || (property == true && valuekey == false))
            {
                _transaction.ValidationErrors.Add(propertyName, errorMessage);
                _transaction.ReturnMessage = _transaction.ReturnMessage + "[" + propertyName + ", " + errorMessage + "]; "
                                             + System.Environment.NewLine + "                                            ";
                _transaction.ErrorLog = _transaction.ReturnMessage;
            }
            _validationStatus = false;
        }

        /// <summary>
        /// Gets value for given business object's property using reflection.
        /// </summary>
        /// <param name="businessObject"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected object GetPropertyValue(string propertyName)
        {
            var ObjectName = propertyName.Contains(".") ? propertyName.Substring(0, propertyName.IndexOf(".")) : "";
            if (ObjectName != "")
            {
                var _varName = propertyName.Substring(propertyName.IndexOf(".") + 1, propertyName.Length - propertyName.IndexOf(".") - 1);
                var _ObjectLista = _businessObject.GetType().GetProperty(ObjectName).GetValue(_businessObject, null);
                return _ObjectLista.GetType().GetProperty(_varName).GetValue(_ObjectLista, null);
            }

            return _businessObject.GetType().GetProperty(propertyName).GetValue(_businessObject, null);
        }

        /// <summary>
        /// Validate Required
        /// </summary>
        /// <param name="propertyName"></param>
        public void ValidateRequired(string propertyName)
        {
            ValidateRequired(propertyName, propertyName);
        }

        /// <summary>
        /// Validate Required
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="friendlyName"></param>
        public void ValidateRequired(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateRequired(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " Es un campo obligatorio.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        public void ValidateName(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidarNombres(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " No debe contener caracteres especiales";
                AddValidationError(propertyName, errorMessage);
            }
        }
        /// <summary>
        /// Validate Guid Required
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="friendlyName"></param>
        public void ValidateGuidRequired(string propertyName, string friendlyName, string displayPropertyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateRequiredGuid(valueOf) == false)
            {
                string errorMessage = friendlyName + " Es un campo obligatorio.";
                if (displayPropertyName == string.Empty)
                {
                    AddValidationError(propertyName, errorMessage);
                }
                else
                {
                    AddValidationError(displayPropertyName, errorMessage);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="friendlyName"></param>
        public void ValidaMayusculas(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.IsToUpper(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " Debe ser en mayúscula.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Tmodel"></typeparam>
        /// <param name="modelo"></param>
        /// <param name="obj"></param>
        /// <param name="ColumnName"></param>
        /// <param name="msj"></param>
       /* public void ValidarCatalogo<Tmodel>(Tmodel modelo, object obj, string ColumnName, string msj)
        {
            var reg = new Utilidades().MapDatos<Catalogos<CatalogoBase>, Tmodel>(modelo);
            foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties())
            {
                string name = propertyInfo.Name;
                object value = propertyInfo.GetValue(obj, null);
                if (name == ColumnName)
                {
                    string codigo = reg.GetCodigo(value.ToString());
                    if (string.IsNullOrEmpty(codigo))
                        AddValidationError(ColumnName, msj);
                    else
                        propertyInfo.SetValue(obj, codigo, null);
                    break;
                }
            }
        }*/

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Tmodel"></typeparam>
        /// <param name="modelo"></param>
        /// <param name="obj"></param>
        /// <param name="ColumnName"></param>
        /// <param name="msj"></param>
       /* public void ValidarCatalogoInt<Tmodel>(Tmodel modelo, object obj, string ColumnName, string msj)
        {

            var reg = new Utilidades().MapDatos<Catalogos<CatalogoBaseInt>, Tmodel>(modelo);
            foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties())
            {
                string name = propertyInfo.Name;
                object value = propertyInfo.GetValue(obj, null);
                if (name == ColumnName)
                {
                    var codigo = reg.GetCodigoInt(value.ToString());
                    if (codigo == 0)
                        AddValidationError(ColumnName, msj);
                    else
                        propertyInfo.SetValue(obj, codigo.ToString(), null);
                    break;
                }
            }
        }*/

        /// <summary>
        /// Validate Length
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateLength(string propertyName, int maxLength)
        {
            ValidateLength(propertyName, propertyName, maxLength);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public void ValidateLengthIgual(string propertyName, int maxLength)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateLengthIgual(valueOf, maxLength) == false)
            {
                _validationStatus = true;
                string errorMessage = propertyName + " Debe ser igual a " + maxLength + " caracteres.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// Validate Length
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateLength(string propertyName, string friendlyName, int maxLength)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateLength(valueOf, maxLength) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " Excede el máximo de " + maxLength + " caracteres.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="friendlyName"></param>
        /// <param name="maximo"></param> 
        public void ValidateNumeroMenorQue(string propertyName, string friendlyName, int maximo)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.IsInteger(valueOf) && ((int)valueOf > maximo))
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " sobrepasa el límite de " + maximo;
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// Validate Numeric
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateNumeber(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.IsInteger(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " No es un número válido.";
                AddValidationError(propertyName, errorMessage);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        public void ValidNumeberString(string propertyName, string friendlyName)
        {
            try
            {
                object valueOf = GetPropertyValue(propertyName);
                Int64 dato = Int64.Parse(valueOf.ToString());
            }
            catch
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " No es un número válido.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eEnumItem"></param>
        /// <param name="propertyName"></param>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        public void ValidateEnum<T>(T eEnumItem, string propertyName, string friendlyName)
        {
            if (!Validations.ValidateEnum(eEnumItem))
            {
                _validationStatus = true;
                string errorMessage = "El campo " + friendlyName + " no es un número valido";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// Validate Greater Than Zero
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateGreaterThanZero(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateGreaterThanZero(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " Debe ser mayor que cero.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// Validate Decimal Greater Than Zero
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateDecimalGreaterThanZero(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateDecimalGreaterThanZero(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " Debe ser mayor que cero.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        public void ValidateMayorIgualZero(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateMayorIgualZero(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " Debe ser mayor o igual a cero.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="friendlyName"></param>
        public void ValidateMayorZero(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateMayorZero(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " Debe ser mayor a cero.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        public void ValidateDecimalIsNotZero(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateDecimalIsNotZero(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " No debe ser igual a cero.";
                AddValidationError(propertyName, errorMessage);
            }
        }


        /// <summary>
        /// Item has a selected value
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateSelectedValue(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.ValidateGreaterThanZero(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " no seleccionado.";
                AddValidationError(propertyName, errorMessage);
            }
        }


        /// <summary>
        /// Validate Is Date
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateIsDate(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.IsDate(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " no es una fecha válida.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// Validate Is Date or Null Date
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateIsDateOrNullDate(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.IsDateOrNullDate(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " no es una fecha válida.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// Validate Required Date
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateRequiredDate(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.IsDateGreaterThanDefaultDate(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " es un campo obligatorio.";
                AddValidationError(propertyName, errorMessage);
            }
        }

        /// <summary>
        /// Validate Date Greater Than or Equal to Today
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="maxLength"></param>
        public void ValidateDateGreaterThanOrEqualToToday(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (Validations.IsDateGreaterThanOrEqualToToday(valueOf) == false)
            {
                _validationStatus = true;
                string errorMessage = friendlyName + " Debe ser mayor o igual que hoy.";
                AddValidationError(propertyName, errorMessage);
            }
        }



        /// <summary>
        /// Validate Email Address
        /// </summary>
        /// <param name="propertyName"></param>
        public void ValidateEmailAddress(string propertyName)
        {
            ValidateEmailAddress(propertyName, propertyName);
        }
        /// <summary>
        /// Validate Email Address
        /// </summary>
        /// <param name="propertyName"></param>
        public void ValidateEmailAddress(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (valueOf != null && Validations.ValidateEmailAddress(valueOf.ToString()) == false)
            {
                _validationStatus = true;
                string emailAddressErrorMessage = friendlyName + " No es una dirección de correo electrónico válida";
                AddValidationError(propertyName, emailAddressErrorMessage);
            }
        }

        /// <summary>
        /// Validatie URL
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        public void ValidateURL(string propertyName, string friendlyName)
        {
            object valueOf = GetPropertyValue(propertyName);
            if (valueOf != null && Validations.ValidateURL(valueOf.ToString()) == false)
            {
                _validationStatus = true;
                string urlErrorMessage = friendlyName + " No es una dirección URL válida";
                AddValidationError(propertyName, urlErrorMessage);
            }
        }
    }
}

