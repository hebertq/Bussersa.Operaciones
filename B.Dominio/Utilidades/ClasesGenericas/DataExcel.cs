using Modelo.ClasesGenericas;
using Modelo.Validaciones;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Utilidades.ClasesGenericas
{
    public static class DataExcel
    {
        public static string CreateExceltoDatatable(DataTable data, string NombreHoja)
        {
            MemoryStream stream = new MemoryStream();
            using (var excelFile = new ExcelPackage(stream))
            {
                var worksheet = excelFile.Workbook.Worksheets.Add(NombreHoja);
                worksheet.Cells[1, 1].LoadFromDataTable(data, true);
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                excelFile.Save();
            }
            return Convert.ToBase64String(Util.ToByteArray(stream));
        }

        public static ExcelArray CreateExcel<T>(List<T> table, string hoja)
        {
            ExcelArray libro = new ExcelArray();
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using ExcelPackage pack = new ExcelPackage();

                ExcelWorksheet ws = pack.Workbook.Worksheets.Add(hoja);
                ws.Cells["A1"].LoadFromCollection(table, true, TableStyles.Light2);               
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                libro.Data = pack.GetAsByteArray();
                libro.Nombre = hoja;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return libro;
        }

        public static string CreateExcel(List<ExcelArray> table)
        {
            string buffer = "";
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using ExcelPackage pack = new ExcelPackage();

                foreach (var item in table)
                {
                    using (MemoryStream memStream = new MemoryStream(item.Data))
                    {
                        using ExcelPackage hija = new ExcelPackage();
                        hija.Load(memStream);
                        pack.Workbook.Worksheets.Add(item.Nombre, hija.Workbook.Worksheets[item.Nombre]);
                    }
                }

                buffer = Convert.ToBase64String(pack.GetAsByteArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return buffer;
        }
    }

    public static class TableHtml
    {
        public static string ToHtmlTable<T>(this List<T> listOfClassObjects, string titulo)
        {
            var ret = string.Empty;
            var response = listOfClassObjects == null || !listOfClassObjects.Any()
                ? ret
                : "<table cellspacing=\"0\" cellpadding=\"0\" border=\"1\">" +
                  listOfClassObjects.First().GetType().GetProperties().Select(p => p.Name).ToList().ToColumnHeaders() +
                  listOfClassObjects.Aggregate(ret, (current, t) => current + t.ToHtmlTableRow()) +
                  "</table>";

            return "<h2>" + titulo + "</h2></hr>" + response;
        }

        private static string ToColumnHeaders<T>(this List<T> listOfProperties)
        {
            var ret = string.Empty;
            return listOfProperties == null || !listOfProperties.Any()
                ? ret
                : "<tr>" +
                  listOfProperties.Aggregate(ret,
                      (current, propValue) =>
                          current +
                          ("<th align = \"center\" valign=\"top\"style='font-size: 11pt; font-weight: bold;min-width:100%;background:linear-gradient(to bottom,#003815 0%,#008559 100%);color:#FFFFFF;padding:5px;'>" +
                           (Convert.ToString(propValue).Length <= 100
                               ? Convert.ToString(propValue)
                               : Convert.ToString(propValue).Substring(0, 100)) + "</th>")) +
                  "</tr>";
        }

        private static string ToHtmlTableRow<T>(this T classObject)
        {
            var ret = string.Empty;
            return classObject == null
                ? ret
                : "<tr>" +
                  classObject.GetType()
                      .GetProperties()
                      .Aggregate(ret,
                          (current, prop) =>
                              current + ("<td align = \"left\" style='font-size: 10pt; font-weight: normal;padding-left:5px;padding-left:5px;padding-right:5px;'>" +
                                         (Convert.ToString(prop.GetValue(classObject, null)).Length <= 100
                                             ? Convert.ToString(prop.GetValue(classObject, null))
                                             : Convert.ToString(prop.GetValue(classObject, null)).Substring(0, 100)) +
                                         "</td>")) + "</tr>";
        }
    }
}
