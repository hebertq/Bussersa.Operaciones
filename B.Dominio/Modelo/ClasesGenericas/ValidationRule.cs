using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.ClasesGenericas
{
    public class ValidationRule<T> where T : class
    {
        public string Message { get; set; }
        public string SuggestionString { get; set; }
        public string PropertyName { get; set; }

        internal Func<T, bool> RuleDelegate { get; set; }
        internal T ObjectTovalidate { get; set; }

        public ValidationRule(Func<T, bool> rule, T arg)
        {
            RuleDelegate = rule;
            ObjectTovalidate = arg;
        }

        public bool RunRuleDelegate()
        {
            return RuleDelegate(ObjectTovalidate);
        }
    }
}
