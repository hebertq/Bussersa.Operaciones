using System;
using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    public class ExcelRequest
    {
        public string Hoja { get; set; } = string.Empty;
        public List<Dictionary<string, object>> Datos { get; set; } = new();
        public bool IncludeHeader { get; set; } = true;
    }

    public class MultiSheetExcelRequest
    {
        public List<ExcelRequest> Hojas { get; set; } = new();
    }
}
