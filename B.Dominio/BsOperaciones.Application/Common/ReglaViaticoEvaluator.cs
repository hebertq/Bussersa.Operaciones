using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Modelo.ClasesGenericas;

namespace BsOperaciones.Application.Common
{
    public class ResultadoEvaluacionViatico
    {
        public bool AplicaRegla { get; set; }
        public bool TieneReglasCliente { get; set; }
        public decimal MontoTransporte { get; set; }
        public decimal MontoAlimentacion { get; set; }
        public decimal BonoBnc { get; set; }
        public bool PagaEnNomina { get; set; }
        public List<string> ReglasAplicadas { get; set; } = new();
        public string Resumen { get; set; } = string.Empty;
    }

    public static class ReglaViaticoEvaluator
    {
        /// <summary>
        /// Evalúa las reglas de viáticos en memoria para un colaborador específico.
        /// No realiza peticiones HTTP.
        /// </summary>
        public static ResultadoEvaluacionViatico Evaluar(
            string? cliente,
            string? area,
            string? horaEntrada,
            string? horaSalida,
            IEnumerable<ReglaViaticoDto>? reglas)
        {
            var resultado = new ResultadoEvaluacionViatico();

            if (reglas == null || !reglas.Any() || string.IsNullOrWhiteSpace(cliente))
            {
                return resultado;
            }

            string clienteNorm = cliente.Trim();
            string areaNorm = area?.Trim() ?? string.Empty;

            // 1. Filtrar reglas activas del cliente
            var reglasCliente = reglas
                .Where(r => r.Activo && string.Equals(r.ClienteNombre?.Trim(), clienteNorm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!reglasCliente.Any())
            {
                resultado.TieneReglasCliente = false;
                return resultado;
            }

            resultado.TieneReglasCliente = true;

            // 2. Determinar reglas candidatas por área (prioridad: área específica, luego comodín '*')
            var reglasCandidatas = reglasCliente
                .Where(r => !string.IsNullOrWhiteSpace(r.Area) && !string.Equals(r.Area.Trim(), "*") &&
                            string.Equals(r.Area.Trim(), areaNorm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!reglasCandidatas.Any())
            {
                reglasCandidatas = reglasCliente
                    .Where(r => string.IsNullOrWhiteSpace(r.Area) || string.Equals(r.Area.Trim(), "*"))
                    .ToList();
            }

            if (!reglasCandidatas.Any())
            {
                return resultado;
            }

            // 3. Parsear horas de entrada y salida del empleado
            TimeSpan entradaEmp = ParseHora(horaEntrada, new TimeSpan(6, 0, 0));
            TimeSpan salidaEmp = ParseHora(horaSalida, new TimeSpan(14, 0, 0));

            // 4. Evaluar cada regla candidata
            var reglasCoincidentes = new List<ReglaViaticoDto>();

            foreach (var regla in reglasCandidatas)
            {
                bool cumple = EvaluarCriterioHorario(regla, entradaEmp, salidaEmp);
                if (cumple)
                {
                    reglasCoincidentes.Add(regla);
                }
            }

            if (!reglasCoincidentes.Any())
            {
                return resultado;
            }

            resultado.AplicaRegla = true;

            foreach (var r in reglasCoincidentes)
            {
                string etiqueta = !string.IsNullOrWhiteSpace(r.NombreRegla) ? r.NombreRegla : $"Regla #{r.Id}";
                resultado.ReglasAplicadas.Add(etiqueta);

                if (r.PagaEnNomina)
                {
                    // Si se paga en nómina, el transporte y la alimentación se suman y van directo a Bono BNC
                    resultado.BonoBnc += (r.MontoTransporte + r.MontoAlimentacion);
                    resultado.PagaEnNomina = true;
                }
                else
                {
                    // Si no se paga en nómina, se aplican a sus columnas respectivas
                    resultado.MontoTransporte += r.MontoTransporte;
                    resultado.MontoAlimentacion += r.MontoAlimentacion;
                }
            }

            if (resultado.PagaEnNomina)
            {
                resultado.Resumen = $"BNC C$ {resultado.BonoBnc:N2} ({string.Join(", ", resultado.ReglasAplicadas)})";
            }
            else
            {
                resultado.Resumen = $"Trasp C$ {resultado.MontoTransporte:N2}, Alim C$ {resultado.MontoAlimentacion:N2} ({string.Join(", ", resultado.ReglasAplicadas)})";
            }

            return resultado;
        }

        private static bool EvaluarCriterioHorario(ReglaViaticoDto regla, TimeSpan entradaEmp, TimeSpan salidaEmp)
        {
            string modo = (regla.ModoCriterioHorario ?? "Siempre").Trim().ToLowerInvariant();

            bool cumpleEntrada = CumpleCondicionRango(entradaEmp, regla.HoraEntradaDesde, regla.HoraEntradaHasta, esEntrada: true);
            bool cumpleSalida = CumpleCondicionRango(salidaEmp, regla.HoraSalidaDesde, regla.HoraSalidaHasta, esEntrada: false);

            switch (modo)
            {
                case "siempre":
                    return true;
                case "soloentrada":
                    return cumpleEntrada;
                case "solosalida":
                    return cumpleSalida;
                case "entradaysalida":
                    return cumpleEntrada && cumpleSalida;
                case "entradaosalida":
                    return cumpleEntrada || cumpleSalida;
                default:
                    return cumpleEntrada && cumpleSalida;
            }
        }

        private static bool CumpleCondicionRango(TimeSpan horaActual, string? desdeStr, string? hastaStr, bool esEntrada)
        {
            TimeSpan? desde = TryParseHoraNullable(desdeStr);
            TimeSpan? hasta = TryParseHoraNullable(hastaStr);

            // Si ambos están vacíos, no hay restricción horaria en este criterio
            if (!desde.HasValue && !hasta.HasValue)
            {
                return true;
            }

            // Si solo se especificó 'hasta', para entrada significa: entró a esa hora o antes (<= hasta)
            // Para salida, si solo se especificó 'hasta', significa salió a esa hora o antes
            if (!desde.HasValue && hasta.HasValue)
            {
                return horaActual <= hasta.Value;
            }

            // Si solo se especificó 'desde', para salida significa: salió a esa hora o después (>= desde)
            // Para entrada, significa entró a esa hora o después (>= desde)
            if (desde.HasValue && !hasta.HasValue)
            {
                return horaActual >= desde.Value;
            }

            // Si ambos tienen valor: rango cerrado
            if (desde.Value <= hasta.Value)
            {
                // Rango diurno normal (ej. 05:00 a 07:00)
                return horaActual >= desde.Value && horaActual <= hasta.Value;
            }
            else
            {
                // Cruce de medianoche (ej. 22:00 a 04:00)
                return horaActual >= desde.Value || horaActual <= hasta.Value;
            }
        }

        private static TimeSpan ParseHora(string? horaStr, TimeSpan defaultVal)
        {
            var res = TryParseHoraNullable(horaStr);
            return res ?? defaultVal;
        }

        private static TimeSpan? TryParseHoraNullable(string? horaStr)
        {
            if (string.IsNullOrWhiteSpace(horaStr)) return null;

            horaStr = horaStr.Trim();
            if (horaStr.Contains(' '))
            {
                var partes = horaStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                horaStr = partes.Length > 1 ? partes[1] : partes[0];
            }

            if (TimeSpan.TryParse(horaStr, CultureInfo.InvariantCulture, out TimeSpan ts))
            {
                return ts;
            }

            string[] formats = { "hh\\:mm", "h\\:mm", "hh\\:mm\\:ss", "h\\:mm\\:ss" };
            if (TimeSpan.TryParseExact(horaStr, formats, CultureInfo.InvariantCulture, out TimeSpan tsExact))
            {
                return tsExact;
            }

            return null;
        }
    }
}
