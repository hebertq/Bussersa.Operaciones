using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BsOperaciones.Application.Common.Interface;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Enum;

namespace BsOperaciones.Application.Common.Strategy.Reloj
{
    public class RelojLasCondeStrategy : RelojStrategyBase, IRelojStrategy
    {
        public TipoReloj Tipo => TipoReloj.LasConde;

        public List<HoraEntrada> Parsear(Stream fileStream, DateTime FchaCarga)
        {
            if (fileStream.CanSeek && fileStream.Position != 0)
                fileStream.Position = 0;

            var marcasCrudas = new List<(int id, DateTime dt)>();

            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string? header = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(header)) return [];

                bool isTabDelimited = header.Contains('\t');
                int enNoIndex = 2;
                int dateTimeIndex = 6;

                if (isTabDelimited)
                {
                    // Detectar dinámicamente índices de EnNo y DateTime en el encabezado GLog
                    var headerTokens = Regex.Split(header.Trim(), @"\t+");
                    int foundEnNo = Array.FindIndex(headerTokens, h => h.Equals("EnNo", StringComparison.OrdinalIgnoreCase));
                    int foundDt = Array.FindIndex(headerTokens, h => h.Equals("DateTime", StringComparison.OrdinalIgnoreCase));

                    if (foundEnNo >= 0) enNoIndex = foundEnNo;
                    if (foundDt >= 0) dateTimeIndex = foundDt;
                }

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (isTabDelimited)
                    {
                        // Formato GLog (delimitado por tabulaciones: No, Mchn, EnNo, Name, Mode, IOMd, DateTime)
                        var tokens = Regex.Split(line.Trim(), @"\t+");
                        int requiredLength = Math.Max(enNoIndex, dateTimeIndex);

                        if (tokens.Length > requiredLength)
                        {
                            string sEnNo = tokens[enNoIndex].Trim();
                            string sDateTime = tokens[dateTimeIndex].Trim();

                            if (int.TryParse(sEnNo, out int idEmpleado) && TryParseDateTime(sDateTime, out DateTime fechaHora))
                            {
                                marcasCrudas.Add((idEmpleado, fechaHora));
                            }
                        }
                    }
                    else
                    {
                        // Formato CSV heredado (separado por comas: sJobNo en index 1, Date en 3 y Time en 4)
                        var columnas = line.Split(',');
                        if (columnas.Length >= 5)
                        {
                            string sJobNo = columnas[1].Trim();
                            string sDate = columnas[3].Trim();
                            string sTime = columnas[4].Trim();

                            if (int.TryParse(sJobNo, out int idEmpleado) && TryParseDateTime($"{sDate} {sTime}", out DateTime fechaHora))
                            {
                                marcasCrudas.Add((idEmpleado, fechaHora));
                            }
                        }
                    }
                }
            }

            // Llamamos a la lógica de la clase base para agrupar entradas y salidas
            return AgruparMarcaciones(marcasCrudas, FchaCarga);
        }

        private static bool TryParseDateTime(string input, out DateTime result)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                result = default;
                return false;
            }

            // Normalizar espacios múltiples (ej: "2026/11/10  12:04:00" -> "2026/11/10 12:04:00")
            string normalized = Regex.Replace(input.Trim(), @"\s+", " ");

            string[] formats =
            [
                "yyyy/MM/dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd H:m:s",
                "yyyy-MM-dd H:m:s",
                "dd/MM/yyyy HH:mm:ss",
                "dd-MM-yyyy HH:mm:ss",
                "yyyy/MM/dd",
                "yyyy-MM-dd"
            ];

            if (DateTime.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;

            return DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
    }
}
