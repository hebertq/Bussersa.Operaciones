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
        public bool TraspAplicaNomina { get; set; }
        public bool AlimAplicaNomina { get; set; }
        public List<string> ReglasAplicadas { get; set; } = new();
        public string Resumen { get; set; } = string.Empty;
    }

    public static class ReglaViaticoEvaluator
    {
        private static bool EsGeneral(string? area)
        {
            if (string.IsNullOrWhiteSpace(area)) return true;
            var a = area.Trim();
            return a == "*" || 
                   string.Equals(a, "TODAS", StringComparison.OrdinalIgnoreCase) || 
                   string.Equals(a, "TODOS", StringComparison.OrdinalIgnoreCase) || 
                   string.Equals(a, "GENERAL", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsMismaArea(string? areaRegla, string? areaColaborador)
        {
            if (EsGeneral(areaRegla)) return false;
            if (string.IsNullOrWhiteSpace(areaColaborador)) return false;
            return string.Equals(areaRegla?.Trim(), areaColaborador?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsSoloEntrada(ReglaViaticoDto r) =>
            string.Equals(r.ModoCriterioHorario?.Trim(), "SoloEntrada", StringComparison.OrdinalIgnoreCase);

        private static bool EsSoloSalida(ReglaViaticoDto r) =>
            string.Equals(r.ModoCriterioHorario?.Trim(), "SoloSalida", StringComparison.OrdinalIgnoreCase);

        private static bool EsMixta(ReglaViaticoDto r) =>
            !EsSoloEntrada(r) && !EsSoloSalida(r);

        /// <summary>
        /// Evalúa las reglas de viáticos en memoria para un colaborador específico:
        /// 1. Prioriza reglas específicas del área sobre reglas generales.
        /// 2. Si un área tiene regla pero esta no aplica por horario y la general sí aplica, hace fallback a la general.
        /// 3. Si tiene regla de entrada y regla de salida, valida y aplica ambas respetando la especificidad del área.
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

            // 2. Parsear horas de entrada y salida del empleado
            TimeSpan entradaEmp = ParseHora(horaEntrada, new TimeSpan(6, 0, 0));
            TimeSpan salidaEmp = ParseHora(horaSalida, new TimeSpan(14, 0, 0));

            // 3. Evaluación de Regla de Entrada (SoloEntrada)
            ReglaViaticoDto? reglaEntradaElegida = null;
            var reglasEntradaArea = reglasCliente.Where(r => EsSoloEntrada(r) && EsMismaArea(r.Area, areaNorm)).ToList();
            if (reglasEntradaArea.Any())
            {
                // Si el área tiene configurada regla específica de entrada, evaluar primero
                reglaEntradaElegida = reglasEntradaArea.FirstOrDefault(r => EvaluarCriterioHorario(r, entradaEmp, salidaEmp));
            }
            // Si el área no tiene regla de entrada, o la que tiene no aplicó por horario, fallback a la general ("todas las áreas")
            if (reglaEntradaElegida == null)
            {
                var reglasEntradaGeneral = reglasCliente.Where(r => EsSoloEntrada(r) && EsGeneral(r.Area)).ToList();
                reglaEntradaElegida = reglasEntradaGeneral.FirstOrDefault(r => EvaluarCriterioHorario(r, entradaEmp, salidaEmp));
            }

            // 4. Evaluación de Regla de Salida (SoloSalida)
            ReglaViaticoDto? reglaSalidaElegida = null;
            var reglasSalidaArea = reglasCliente.Where(r => EsSoloSalida(r) && EsMismaArea(r.Area, areaNorm)).ToList();
            if (reglasSalidaArea.Any())
            {
                // Si el área tiene configurada regla específica de salida, evaluar primero
                reglaSalidaElegida = reglasSalidaArea.FirstOrDefault(r => EvaluarCriterioHorario(r, entradaEmp, salidaEmp));
            }
            // Si el área no tiene regla de salida, o la que tiene no aplicó por horario, fallback a la general ("todas las áreas")
            if (reglaSalidaElegida == null)
            {
                var reglasSalidaGeneral = reglasCliente.Where(r => EsSoloSalida(r) && EsGeneral(r.Area)).ToList();
                reglaSalidaElegida = reglasSalidaGeneral.FirstOrDefault(r => EvaluarCriterioHorario(r, entradaEmp, salidaEmp));
            }

            // 5. Evaluación de Regla Mixta / Jornada Completa (EntradaYSalida, EntradaOSalida, Siempre)
            ReglaViaticoDto? reglaMixtaElegida = null;
            var reglasMixtasArea = reglasCliente.Where(r => EsMixta(r) && EsMismaArea(r.Area, areaNorm)).ToList();
            if (reglasMixtasArea.Any())
            {
                reglaMixtaElegida = reglasMixtasArea.FirstOrDefault(r => EvaluarCriterioHorario(r, entradaEmp, salidaEmp));
            }
            if (reglaMixtaElegida == null)
            {
                var reglasMixtasGeneral = reglasCliente.Where(r => EsMixta(r) && EsGeneral(r.Area)).ToList();
                reglaMixtaElegida = reglasMixtasGeneral.FirstOrDefault(r => EvaluarCriterioHorario(r, entradaEmp, salidaEmp));
            }

            // 6. Consolidación de reglas coincidentes
            var reglasCoincidentes = new List<ReglaViaticoDto>();

            if (reglaEntradaElegida != null)
            {
                reglasCoincidentes.Add(reglaEntradaElegida);
            }

            if (reglaSalidaElegida != null)
            {
                if (!reglasCoincidentes.Any(r => r.Id == reglaSalidaElegida.Id))
                {
                    reglasCoincidentes.Add(reglaSalidaElegida);
                }
            }

            if (reglaMixtaElegida != null)
            {
                if (reglaEntradaElegida == null && reglaSalidaElegida == null)
                {
                    reglasCoincidentes.Add(reglaMixtaElegida);
                }
                else if (EsMismaArea(reglaMixtaElegida.Area, areaNorm))
                {
                    // Si el área tiene regla mixta específica que cumplió, prevalece sobre reglas generales
                    reglasCoincidentes.RemoveAll(r => EsGeneral(r.Area));
                    if (!reglasCoincidentes.Any(r => r.Id == reglaMixtaElegida.Id))
                    {
                        reglasCoincidentes.Add(reglaMixtaElegida);
                    }
                }
                else
                {
                    // Si la regla mixta es general, solo reemplaza si entrada y salida eran ambas generales
                    bool tieneEntradaArea = reglaEntradaElegida != null && EsMismaArea(reglaEntradaElegida.Area, areaNorm);
                    bool tieneSalidaArea = reglaSalidaElegida != null && EsMismaArea(reglaSalidaElegida.Area, areaNorm);

                    if (!tieneEntradaArea && !tieneSalidaArea)
                    {
                        reglasCoincidentes.RemoveAll(r => EsGeneral(r.Area));
                        reglasCoincidentes.Add(reglaMixtaElegida);
                    }
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
                string origen = !string.IsNullOrWhiteSpace(r.Area) && !EsGeneral(r.Area) ? $"{etiqueta} ({r.Area})" : $"{etiqueta} (General)";
                resultado.ReglasAplicadas.Add(origen);

                // Montos de transporte y alimentación siempre se asignan para cobro al cliente (Ticket de Viáticos)
                resultado.MontoTransporte += r.MontoTransporte;
                resultado.MontoAlimentacion += r.MontoAlimentacion;

                if (r.PagaEnNomina)
                {
                    if (r.MontoTransporte > 0) resultado.TraspAplicaNomina = true;
                    if (r.MontoAlimentacion > 0) resultado.AlimAplicaNomina = true;
                    resultado.PagaEnNomina = true;
                }
            }

            // Bono BNC suma transporte y alimentación siempre que estén marcados para aplicar en nómina
            resultado.BonoBnc = (resultado.TraspAplicaNomina ? resultado.MontoTransporte : 0) +
                                (resultado.AlimAplicaNomina ? resultado.MontoAlimentacion : 0);

            if (resultado.PagaEnNomina)
            {
                resultado.Resumen = $"Trasp C$ {resultado.MontoTransporte:N2}, Alim C$ {resultado.MontoAlimentacion:N2} -> BNC Nómina C$ {resultado.BonoBnc:N2} [{string.Join(" + ", resultado.ReglasAplicadas)}]";
            }
            else
            {
                resultado.Resumen = $"Trasp C$ {resultado.MontoTransporte:N2}, Alim C$ {resultado.MontoAlimentacion:N2} -> Efectivo [{string.Join(" + ", resultado.ReglasAplicadas)}]";
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
