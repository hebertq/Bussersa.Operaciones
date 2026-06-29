using Modelo.ClasesGenericas;
using Modelo.Validaciones;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Utilidades.ClasesGenericas
{
    public static class DataExcel
    {
        private static byte[] _logoBytesCache = null;
        private static readonly object _lock = new object();

        private static byte[] LoadLogoBytes()
        {
            if (_logoBytesCache != null)
                return _logoBytesCache;

            lock (_lock)
            {
                if (_logoBytesCache != null)
                    return _logoBytesCache;

                try
                {
                    var assembly = typeof(DataExcel).Assembly;
                    string resourceName = assembly.GetManifestResourceNames()
                        .FirstOrDefault(x => x.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase));

                    if (resourceName != null)
                    {
                        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream != null)
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    _logoBytesCache = ms.ToArray();
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            return _logoBytesCache;
        }

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

        public static ExcelArray CreateExcel<T>(List<T> table, string hoja, bool includeHeader = true)
        {
            ExcelArray libro = new ExcelArray();
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using ExcelPackage pack = new ExcelPackage();

                ExcelWorksheet ws = pack.Workbook.Worksheets.Add(hoja);
                
                if (includeHeader)
                {
                    // Load data collection starting at A4 to leave space for logo and title
                    ws.Cells["A4"].LoadFromCollection(table, true, TableStyles.Light2);               

                    // Add Logo to the Worksheet
                    try
                    {
                        byte[] logoBytes = LoadLogoBytes();
                        if (logoBytes != null)
                        {
                            using (MemoryStream imageStream = new MemoryStream(logoBytes))
                            {
                                var picture = ws.Drawings.AddPicture("Logo_" + Guid.NewGuid().ToString().Substring(0, 8), imageStream);
                                picture.SetPosition(0, 5, 0, 5); // Row 1, Column A
                                picture.SetSize(105, 70);
                            }
                        }
                        else
                        {
                            // Fallback to disk file if assembly resource is not found
                            string logoPath = "logo.png";
                            if (!File.Exists(logoPath))
                            {
                                logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                            }
                            if (!File.Exists(logoPath))
                            {
                                logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot/img/brand/logo.png");
                            }
                            if (!File.Exists(logoPath))
                            {
                                logoPath = "wwwroot/img/brand/logo.png";
                            }

                            if (File.Exists(logoPath))
                            {
                                var picture = ws.Drawings.AddPicture("Logo_" + Guid.NewGuid().ToString().Substring(0, 8), new FileInfo(logoPath));
                                picture.SetPosition(0, 5, 0, 5); // Row 1, Column A
                                picture.SetSize(105, 70);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error al agregar logo a excel: " + ex.Message);
                    }

                    // Add Title and Subtitle
                    try
                    {
                        ws.Cells["D2:H2"].Merge = true;
                        ws.Cells["D2"].Value = "BUSSERSA";
                        ws.Cells["D2"].Style.Font.Size = 14;
                        ws.Cells["D2"].Style.Font.Bold = true;

                        ws.Cells["D3:H3"].Merge = true;
                        ws.Cells["D3"].Value = "Reporte: " + hoja;
                        ws.Cells["D3"].Style.Font.Size = 10;
                        ws.Cells["D3"].Style.Font.Italic = true;
                        ws.Cells["D3"].Style.Font.Color.SetColor(System.Drawing.Color.Gray); // Color gris
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error al agregar texto a excel: " + ex.Message);
                    }
                }
                else
                {
                    // Load starting at A1 (headers on row 1)
                    ws.Cells["A1"].LoadFromCollection(table, true, TableStyles.Light2);
                }

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

        public static string CreateExcel(List<(System.Collections.IEnumerable list, string sheetName)> sheets, bool includeHeader = true)
        {
            string buffer = "";
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using ExcelPackage pack = new ExcelPackage();

                foreach (var sheet in sheets)
                {
                    ExcelWorksheet ws = pack.Workbook.Worksheets.Add(sheet.sheetName);
                    
                    if (includeHeader)
                    {
                        // Load data collection starting at A4 to leave space for logo and title
                        ws.Cells["A4"].LoadFromCollection(sheet.list.Cast<object>(), true, TableStyles.Light2);               

                        // Add Logo to the Worksheet
                        try
                        {
                            byte[] logoBytes = LoadLogoBytes();
                            if (logoBytes != null)
                            {
                                using (MemoryStream imageStream = new MemoryStream(logoBytes))
                                {
                                    var picture = ws.Drawings.AddPicture("Logo_" + Guid.NewGuid().ToString().Substring(0, 8), imageStream);
                                    picture.SetPosition(0, 5, 0, 5); // Row 1, Column A
                                    picture.SetSize(105, 70);
                                }
                            }
                            else
                            {
                                string logoPath = "logo.png";
                                if (!File.Exists(logoPath))
                                {
                                    logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                                }
                                if (!File.Exists(logoPath))
                                {
                                    logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot/img/brand/logo.png");
                                }
                                if (!File.Exists(logoPath))
                                {
                                    logoPath = "wwwroot/img/brand/logo.png";
                                }

                                if (File.Exists(logoPath))
                                {
                                    var picture = ws.Drawings.AddPicture("Logo_" + Guid.NewGuid().ToString().Substring(0, 8), new FileInfo(logoPath));
                                    picture.SetPosition(0, 5, 0, 5); // Row 1, Column A
                                    picture.SetSize(105, 70);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error al agregar logo a excel: " + ex.Message);
                        }

                        // Add Title and Subtitle
                        try
                        {
                            ws.Cells["D2:H2"].Merge = true;
                            ws.Cells["D2"].Value = "BUSSERSA";
                            ws.Cells["D2"].Style.Font.Size = 14;
                            ws.Cells["D2"].Style.Font.Bold = true;

                            ws.Cells["D3:H3"].Merge = true;
                            ws.Cells["D3"].Value = "Reporte: " + sheet.sheetName;
                            ws.Cells["D3"].Style.Font.Size = 10;
                            ws.Cells["D3"].Style.Font.Italic = true;
                            ws.Cells["D3"].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error al agregar texto a excel: " + ex.Message);
                        }
                    }
                    else
                    {
                        ws.Cells["A1"].LoadFromCollection(sheet.list.Cast<object>(), true, TableStyles.Light2);
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                buffer = Convert.ToBase64String(pack.GetAsByteArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return buffer;
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

        public static byte[] GenerateExcelFromJson(MultiSheetExcelRequest request)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using ExcelPackage pack = new ExcelPackage();

            foreach (var sheetReq in request.Hojas)
            {
                ExcelWorksheet ws = pack.Workbook.Worksheets.Add(sheetReq.Hoja);
                
                if (sheetReq.Datos == null || !sheetReq.Datos.Any())
                    continue;

                // Get headers (keys from first row)
                var headers = sheetReq.Datos.First().Keys.ToList();
                int startRow = sheetReq.IncludeHeader ? 5 : 1;

                // Write headers
                for (int col = 0; col < headers.Count; col++)
                {
                    ws.Cells[startRow, col + 1].Value = headers[col];
                    ws.Cells[startRow, col + 1].Style.Font.Bold = true;
                }

                // Write data rows
                int currentRow = startRow + 1;
                foreach (var rowData in sheetReq.Datos)
                {
                    for (int col = 0; col < headers.Count; col++)
                    {
                        var key = headers[col];
                        var value = rowData.ContainsKey(key) ? rowData[key] : null;

                        // Check if value is a JSON Element (due to HTTP deserialization)
                        if (value is System.Text.Json.JsonElement jsonEl)
                        {
                            switch (jsonEl.ValueKind)
                            {
                                case System.Text.Json.JsonValueKind.String:
                                    ws.Cells[currentRow, col + 1].Value = jsonEl.GetString();
                                    break;
                                case System.Text.Json.JsonValueKind.Number:
                                    if (jsonEl.TryGetInt64(out long lVal))
                                        ws.Cells[currentRow, col + 1].Value = lVal;
                                    else if (jsonEl.TryGetDouble(out double dVal))
                                        ws.Cells[currentRow, col + 1].Value = dVal;
                                    break;
                                case System.Text.Json.JsonValueKind.True:
                                    ws.Cells[currentRow, col + 1].Value = true;
                                    break;
                                case System.Text.Json.JsonValueKind.False:
                                    ws.Cells[currentRow, col + 1].Value = false;
                                    break;
                                case System.Text.Json.JsonValueKind.Null:
                                    ws.Cells[currentRow, col + 1].Value = null;
                                    break;
                                default:
                                    ws.Cells[currentRow, col + 1].Value = jsonEl.ToString();
                                    break;
                            }
                        }
                        else
                        {
                            ws.Cells[currentRow, col + 1].Value = value;
                        }
                    }
                    currentRow++;
                }

                // Add table style
                if (currentRow > startRow + 1)
                {
                    var dataRange = ws.Cells[startRow, 1, currentRow - 1, headers.Count];
                    var tbl = ws.Tables.Add(dataRange, "Table_" + Guid.NewGuid().ToString().Substring(0, 8));
                    tbl.TableStyle = OfficeOpenXml.Table.TableStyles.Light2;
                    tbl.ShowHeader = true;
                }

                if (sheetReq.IncludeHeader)
                {
                    // Add Logo
                    try
                    {
                        byte[] logoBytes = LoadLogoBytes();
                        if (logoBytes != null)
                        {
                            using (MemoryStream imageStream = new MemoryStream(logoBytes))
                            {
                                var picture = ws.Drawings.AddPicture("Logo_" + Guid.NewGuid().ToString().Substring(0, 8), imageStream);
                                picture.SetPosition(0, 5, 0, 5); // Row 1, Column A
                                picture.SetSize(105, 70);
                            }
                        }
                        else
                        {
                            // Fallback to disk file if assembly resource is not found
                            string logoPath = "logo.png";
                            if (!File.Exists(logoPath))
                            {
                                logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                            }
                            if (!File.Exists(logoPath))
                            {
                                logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot/img/brand/logo.png");
                            }
                            if (!File.Exists(logoPath))
                            {
                                logoPath = "wwwroot/img/brand/logo.png";
                            }

                            if (File.Exists(logoPath))
                            {
                                var picture = ws.Drawings.AddPicture("Logo_" + Guid.NewGuid().ToString().Substring(0, 8), new FileInfo(logoPath));
                                picture.SetPosition(0, 5, 0, 5); // Row 1, Column A
                                picture.SetSize(105, 70);
                            }
                        }
                    }
                    catch { }

                    // Add Title
                    ws.Cells["D2:H2"].Merge = true;
                    ws.Cells["D2"].Value = "BUSSERSA";
                    ws.Cells["D2"].Style.Font.Size = 14;
                    ws.Cells["D2"].Style.Font.Bold = true;

                    ws.Cells["D3:H3"].Merge = true;
                    ws.Cells["D3"].Value = "Reporte: " + sheetReq.Hoja;
                    ws.Cells["D3"].Style.Font.Size = 10;
                    ws.Cells["D3"].Style.Font.Italic = true;
                    ws.Cells["D3"].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            return pack.GetAsByteArray();
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
