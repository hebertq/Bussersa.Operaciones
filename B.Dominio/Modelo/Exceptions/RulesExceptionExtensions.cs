using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Modelo.Exceptions
{
    public static class RulesExceptionExtensions
    {
        public static void AddModelStateErrors(this RulesException ex, ModelStateDictionary modelState)
        {
            foreach (var error in ex.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        public static void AddModelStateErrors(this IEnumerable<RulesException> errors, ModelStateDictionary modelState)
        {
            foreach (RulesException ex in errors)
            {
                ex.AddModelStateErrors(modelState);
            }
        }

        public static IEnumerable Errors(this ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
            {
                return modelState.ToDictionary(kvp => kvp.Key.ToCamelCase(),
                    kvp => kvp.Value
                        .Errors
                        .Select(e => e.ErrorMessage).ToArray())
                        .Where(m => m.Value.Count() > 0);
            }
            return null;
        }

        public static IEnumerable Errors(this ModelStateDictionary modelState, bool fixName = false)
        {
            if (!modelState.IsValid)
            {
                return modelState.ToDictionary(kvp => fixName ? kvp.Key.Replace("model.", string.Empty).ToCamelCase() : kvp.Key.ToCamelCase(),
                    kvp => kvp.Value
                        .Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor no válido ingresado." : e.ErrorMessage).ToArray())
                        .Where(m => m.Value.Count() > 0);
            }
            return null;
        }
    }

    public static class StringExtension
    {
        public static string ToCamelCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > 1)
            {
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
            }
            return str.ToLowerInvariant();
        }
    }
}
