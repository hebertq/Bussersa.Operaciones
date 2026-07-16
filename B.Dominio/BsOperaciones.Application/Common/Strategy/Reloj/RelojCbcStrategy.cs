using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BsOperaciones.Application.Common.Interface;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Enum;

namespace BsOperaciones.Application.Common.Strategy.Reloj
{
    public class RelojCbcStrategy : RelojStrategyBase, IRelojStrategy
    {
        public TipoReloj Tipo => TipoReloj.Cbc;

        public List<HoraEntrada> Parsear(Stream fileStream, DateTime FchaCarga)
        {
            var marcasCrudas = new List<(int id, DateTime dt)>();

            try
            {
                // Cargar el XML Spreadsheet 2003
                var doc = XDocument.Load(fileStream);
                XNamespace ss = "urn:schemas-microsoft-com:office:spreadsheet";

                var worksheet = doc.Root?.Elements(ss + "Worksheet")
                    .FirstOrDefault(w => w.Attribute(ss + "Name")?.Value == "Punch");

                if (worksheet == null) return new List<HoraEntrada>();

                var table = worksheet.Element(ss + "Table");
                if (table == null) return new List<HoraEntrada>();

                var rows = table.Elements(ss + "Row").ToList();

                int? currentId = null;
                int targetColIndex = -1;

                // Modos de escaneo en la máquina de estados:
                // 0: Esperando fila de información de empleado (ej. "ID:00000004...")
                // 1: Esperando fila de días (ej. "12 | 13 | 14 | 15")
                // 2: Esperando fila de marcaciones (ej. "09:08 17:09 | ...")
                int scanState = 0;

                foreach (var row in rows)
                {
                    var cells = row.Elements(ss + "Cell").ToList();
                    if (!cells.Any())
                    {
                        // Si encontramos una fila vacía, reiniciamos el estado por seguridad
                        currentId = null;
                        targetColIndex = -1;
                        scanState = 0;
                        continue;
                    }

                    // Obtenemos los valores de celdas con soporte de ss:Index
                    var cellValues = GetCellValues(cells, ss);

                    if (scanState == 0)
                    {
                        // Buscamos si la primera celda contiene "ID:"
                        var firstCellText = cellValues.FirstOrDefault() ?? "";
                        var idMatch = Regex.Match(firstCellText, @"ID:\s*(\d+)");
                        if (idMatch.Success)
                        {
                            currentId = int.Parse(idMatch.Groups[1].Value);
                            scanState = 1; // Pasamos a esperar los días
                        }
                    }
                    else if (scanState == 1)
                    {
                        // Buscamos en qué columna (index) está el día que nos interesa cargar
                        targetColIndex = -1;
                        var dayToFind = FchaCarga.Day.ToString();

                        for (int colIdx = 0; colIdx < cellValues.Count; colIdx++)
                        {
                            if (cellValues[colIdx]?.Trim() == dayToFind)
                            {
                                targetColIndex = colIdx;
                                break;
                            }
                        }

                        // Si encontramos el día, pasamos a esperar las marcaciones.
                        // Si no, volvemos a buscar el siguiente empleado (por si acaso el archivo no tiene ese día).
                        if (targetColIndex != -1)
                        {
                            scanState = 2;
                        }
                        else
                        {
                            currentId = null;
                            scanState = 0;
                        }
                    }
                    else if (scanState == 2)
                    {
                        if (currentId != null && targetColIndex != -1 && targetColIndex < cellValues.Count)
                        {
                            var punchText = cellValues[targetColIndex];
                            if (!string.IsNullOrWhiteSpace(punchText))
                            {
                                // Las marcaciones vienen separadas por espacio
                                var parts = punchText.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var part in parts)
                                {
                                    if (TimeSpan.TryParse(part.Trim(), out TimeSpan hora))
                                    {
                                        marcasCrudas.Add((currentId.Value, FchaCarga.Date.Add(hora)));
                                    }
                                }
                            }
                        }

                        // Ya procesamos este bloque de empleado, reiniciamos el estado para el siguiente
                        currentId = null;
                        targetColIndex = -1;
                        scanState = 0;
                    }
                }
            }
            catch (Exception)
            {
                // En producción es mejor no romper y retornar lista vacía si el formato de archivo es inválido
                return new List<HoraEntrada>();
            }

            return AgruparMarcaciones(marcasCrudas, FchaCarga);
        }

        /// <summary>
        /// Mapea las celdas secuencialmente tomando en cuenta el atributo Index (base 1) en XML Spreadsheet 2003.
        /// </summary>
        private List<string> GetCellValues(List<XElement> cells, XNamespace ss)
        {
            var values = new List<string>();
            int expectedIndex = 0;

            foreach (var cell in cells)
            {
                var indexAttr = cell.Attribute(ss + "Index");
                if (indexAttr != null && int.TryParse(indexAttr.Value, out int idx))
                {
                    expectedIndex = idx - 1; // XML es base 1, C# es base 0
                }

                // Rellenamos con celdas vacías si hay saltos de columnas
                while (values.Count < expectedIndex)
                {
                    values.Add(string.Empty);
                }

                var dataVal = cell.Element(ss + "Data")?.Value ?? string.Empty;
                values.Add(dataVal);
                expectedIndex++;
            }

            return values;
        }
    }
}
