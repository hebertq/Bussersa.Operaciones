using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using BsOperaciones.Application.Common.Interface;
using ExcelDataReader;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Enum;

namespace BsOperaciones.Application.Common.Strategy.Reloj
{
    public class RelojSinsaStrategy : RelojStrategyBase, IRelojStrategy
    {
        public TipoReloj Tipo => TipoReloj.Sinsa;

        static RelojSinsaStrategy()
        {
            // Registrar proveedor para páginas de códigos (1252, etc.) en archivos .xls legacy
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public List<HoraEntrada> Parsear(Stream fileStream, DateTime FchaCarga)
        {
            if (fileStream.CanSeek && fileStream.Position != 0)
                fileStream.Position = 0;

            var marcasCrudas = new List<(int id, DateTime dt)>();

            using var reader = ExcelReaderFactory.CreateReader(fileStream);
            var ds = reader.AsDataSet();

            if (ds == null || ds.Tables.Count == 0) return [];

            // 1. FORMATO A: Matriz diaria ("Attendance Record" / "Reporte de Asistencia" por columnas de días)
            foreach (DataTable table in ds.Tables)
            {
                if (table.TableName.IndexOf("Record", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    table.TableName.IndexOf("Asistencia", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ds.Tables.Count == 1)
                {
                    int? diaCol = null;
                    int headerRow = -1;

                    // Buscar la fila de encabezado que contiene los días dentro de las primeras 10 filas
                    for (int r = 0; r < Math.Min(10, table.Rows.Count); r++)
                    {
                        for (int c = 3; c < table.Columns.Count; c++)
                        {
                            var val = table.Rows[r][c]?.ToString()?.Trim();
                            if (int.TryParse(val, out int dia) && dia == FchaCarga.Day)
                            {
                                diaCol = c;
                                headerRow = r;
                                break;
                            }
                        }
                        if (diaCol != null) break;
                    }

                    if (diaCol != null)
                    {
                        for (int r = headerRow + 1; r < table.Rows.Count; r++)
                        {
                            var idStr = table.Rows[r][0]?.ToString()?.Trim();
                            if (int.TryParse(idStr, out int id))
                            {
                                var hStr = table.Rows[r][diaCol.Value]?.ToString();
                                var horas = ExtraerHorasDeTexto(hStr);
                                foreach (var h in horas)
                                {
                                    marcasCrudas.Add((id, FchaCarga.Date.Add(h)));
                                }
                            }
                        }
                    }
                }
            }

            // 2. FORMATO B: Standard Report ZKTeco (Fila con "ID: <id>" y fila impar con marcaciones concatenadas)
            if (!marcasCrudas.Any())
            {
                foreach (DataTable table in ds.Tables)
                {
                    if (table.TableName.IndexOf("Asistencia", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        table.TableName.IndexOf("Standard", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int? currentId = null;
                        for (int r = 0; r < table.Rows.Count; r++)
                        {
                            var c0 = table.Rows[r][0]?.ToString()?.Trim();

                            if (!string.IsNullOrEmpty(c0) && c0.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
                            {
                                currentId = null;
                                for (int col = 1; col < Math.Min(5, table.Columns.Count); col++)
                                {
                                    var idValStr = table.Rows[r][col]?.ToString()?.Trim();
                                    if (int.TryParse(idValStr, out int idVal))
                                    {
                                        currentId = idVal;
                                        break;
                                    }
                                }
                            }
                            else if (currentId != null && !string.IsNullOrWhiteSpace(c0) && !c0.Contains("ID"))
                            {
                                var horas = ExtraerHorasDeTexto(c0);
                                foreach (var h in horas)
                                {
                                    marcasCrudas.Add((currentId.Value, FchaCarga.Date.Add(h)));
                                }
                                currentId = null;
                            }
                        }
                    }
                }
            }

            // 3. FORMATO C: Tabular de excepciones ("Reporte de Excepciones" / "Attendance Abnormal")
            if (!marcasCrudas.Any())
            {
                foreach (DataTable table in ds.Tables)
                {
                    if (table.TableName.IndexOf("Excepcion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        table.TableName.IndexOf("Abnormal", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        for (int r = 0; r < table.Rows.Count; r++)
                        {
                            var idStr = table.Rows[r][0]?.ToString()?.Trim();
                            if (int.TryParse(idStr, out int id))
                            {
                                // Col 3: Fecha, Col 4: Entrada, Col 5: Salida
                                string? fStr = table.Columns.Count > 3 ? table.Rows[r][3]?.ToString()?.Trim() : null;
                                string? inStr = table.Columns.Count > 4 ? table.Rows[r][4]?.ToString()?.Trim() : null;
                                string? outStr = table.Columns.Count > 5 ? table.Rows[r][5]?.ToString()?.Trim() : null;

                                bool fechaCoincide = true;
                                if (!string.IsNullOrEmpty(fStr) && DateTime.TryParse(fStr, out DateTime rowDate))
                                {
                                    if (rowDate.Date != FchaCarga.Date) fechaCoincide = false;
                                }

                                if (fechaCoincide)
                                {
                                    var horasIn = ExtraerHorasDeTexto(inStr);
                                    foreach (var h in horasIn)
                                        marcasCrudas.Add((id, FchaCarga.Date.Add(h)));

                                    var horasOut = ExtraerHorasDeTexto(outStr);
                                    foreach (var h in horasOut)
                                        marcasCrudas.Add((id, FchaCarga.Date.Add(h)));
                                }
                            }
                        }
                    }
                }
            }

            return AgruparMarcaciones(marcasCrudas, FchaCarga);
        }

        private static List<TimeSpan> ExtraerHorasDeTexto(string? texto)
        {
            var resultado = new List<TimeSpan>();
            if (string.IsNullOrWhiteSpace(texto)) return resultado;

            var partes = texto.Split(new[] { '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var parte in partes)
            {
                var token = parte.Trim();
                if (token == "--:--" || token == "00:00") continue;

                if (TimeSpan.TryParse(token, out TimeSpan ts))
                {
                    resultado.Add(ts);
                }
                else if (token.Length >= 10 && token.Length % 5 == 0 && token.Contains(':'))
                {
                    for (int i = 0; i + 5 <= token.Length; i += 5)
                    {
                        var sub = token.Substring(i, 5);
                        if (TimeSpan.TryParse(sub, out TimeSpan subTs))
                        {
                            resultado.Add(subTs);
                        }
                    }
                }
            }
            return resultado.Distinct().ToList();
        }
    }
}
