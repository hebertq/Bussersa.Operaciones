using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Modelo.Report;

namespace BsOperaciones.Application.Common.Strategy.Conciliacion
{
    public class ConciliacionSinsaStrategy : IConciliacionClienteStrategy
    {
        public int OperacionId => 23;
        public string NombreCliente => "Sinsa";
        public string NombreFormatoArchivo => "Gastos de Terceros Sinsa (.xlsx)";

        public bool SoportaOperacion(int operacionId, string? nombreOperacion)
        {
            if (operacionId == OperacionId) return true;
            if (!string.IsNullOrWhiteSpace(nombreOperacion) &&
                nombreOperacion.IndexOf("sinsa", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        private class SinsaItem
        {
            public DateTime Fecha { get; set; }
            public string Empleado { get; set; } = string.Empty;
            public string EmpleadoNorm { get; set; } = string.Empty;
            public string Puesto { get; set; } = string.Empty;
            public string Bodega { get; set; } = string.Empty;
            public decimal HorasOrd { get; set; }
            public decimal HorasExt { get; set; }
            public decimal TotalHoras => HorasOrd + HorasExt;
            public string EstadoPago { get; set; } = "PAGADO";
            public string NoFactura { get; set; } = string.Empty;
            public string NoControl { get; set; } = string.Empty;
        }

        public ResultadoConciliacionDto Conciliar(Stream fileStream, List<ReporteCierreMarcadasDetalle> marcadasBussersa, DateTime? fchaInicio, DateTime? fchaFin)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var resultado = new ResultadoConciliacionDto
            {
                NombreCliente = NombreCliente,
                FechaInicio = fchaInicio,
                FechaFin = fchaFin
            };

            // 1. Leer el archivo Excel del cliente (Sinsa)
            using var reader = ExcelReaderFactory.CreateReader(fileStream);
            var ds = reader.AsDataSet();

            DataTable? table = null;
            foreach (DataTable dt in ds.Tables)
            {
                if (dt.TableName.IndexOf("Terceros", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dt.TableName.IndexOf("Base", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    table = dt;
                    break;
                }
            }
            if (table == null && ds.Tables.Count > 0)
            {
                table = ds.Tables[0];
            }

            if (table == null || table.Rows.Count <= 1)
            {
                return resultado;
            }

            // Identificar índices de columnas dinámicamente o por posición estándar
            int colFecha = 4, colEmpleado = 7, colPuesto = 8, colBodega = 1;
            int colHorasOrd = 12, colHorasExt = 13, colEstadoPago = 20, colFactura = 22, colControl = 19;

            var r0 = table.Rows[0];
            for (int c = 0; c < table.Columns.Count; c++)
            {
                string h = r0[c]?.ToString()?.Trim()?.ToLowerInvariant() ?? "";
                if (h.Contains("empleado") || h.Contains("colaborador") || h.Contains("trabajador")) colEmpleado = c;
                else if (h.Contains("fecha") && !h.Contains("pago")) colFecha = c;
                else if (h.Contains("puesto") || h.Contains("cargo")) colPuesto = c;
                else if (h.Contains("bodega") || h.Contains("zona")) colBodega = c;
                else if ((h.Contains("ordinaria") || h.Contains("normal")) && !h.Contains("costo") && !h.Contains("tarifa") && !h.Contains("valor") && !h.Contains("monto")) colHorasOrd = c;
                else if (h.Contains("extra") && !h.Contains("costo") && !h.Contains("tarifa") && !h.Contains("valor") && !h.Contains("monto")) colHorasExt = c;
                else if (h.Contains("estado")) colEstadoPago = c;
                else if (h.Contains("factura")) colFactura = c;
                else if (h.Contains("control")) colControl = c;
            }

            var sinsaItems = new List<SinsaItem>();
            for (int r = 1; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];
                var emp = row[colEmpleado]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(emp)) continue;

                var fStr = row[colFecha]?.ToString()?.Trim() ?? "";
                if (!DateTime.TryParse(fStr, out DateTime f)) continue;

                // Filtrar por rango si está especificado
                if (fchaInicio.HasValue && f.Date < fchaInicio.Value.Date) continue;
                if (fchaFin.HasValue && f.Date > fchaFin.Value.Date) continue;

                decimal ord = 0, ext = 0;
                string sOrd = row[colHorasOrd]?.ToString()?.Replace(",", ".")?.Trim() ?? "0";
                string sExt = row[colHorasExt]?.ToString()?.Replace(",", ".")?.Trim() ?? "0";
                decimal.TryParse(sOrd, NumberStyles.Any, CultureInfo.InvariantCulture, out ord);
                decimal.TryParse(sExt, NumberStyles.Any, CultureInfo.InvariantCulture, out ext);

                string estado = row[colEstadoPago]?.ToString()?.Trim()?.ToUpperInvariant() ?? "PAGADO";
                string fac = row[colFactura]?.ToString()?.Trim() ?? "";
                string ctl = row[colControl]?.ToString()?.Trim() ?? "";
                string puesto = row[colPuesto]?.ToString()?.Trim() ?? "";
                string bodega = row[colBodega]?.ToString()?.Trim() ?? "";

                sinsaItems.Add(new SinsaItem
                {
                    Fecha = f.Date,
                    Empleado = emp,
                    EmpleadoNorm = NormalizarNombre(emp),
                    Puesto = puesto,
                    Bodega = bodega,
                    HorasOrd = ord,
                    HorasExt = ext,
                    EstadoPago = estado.Contains("PENDIENTE") ? "PENDIENTE" : "PAGADO",
                    NoFactura = fac,
                    NoControl = ctl
                });
            }

            // 2. Resumen General de Bussersa
            var bussersaFiltradas = marcadasBussersa ?? new List<ReporteCierreMarcadasDetalle>();
            resultado.TotalTurnosBussersa = bussersaFiltradas.Count;
            resultado.TotalHorasOrdBussersa = Math.Round(bussersaFiltradas.Sum(x => x.HorasNormalesCobrables), 2);
            resultado.TotalHorasExtBussersa = Math.Round(bussersaFiltradas.Sum(x => x.HorasExtrasCobrables), 2);
            resultado.TotalHorasBussersa = resultado.TotalHorasOrdBussersa + resultado.TotalHorasExtBussersa;

            // 3. Resumen General del Cliente (Sinsa)
            var pagados = sinsaItems.Where(x => x.EstadoPago == "PAGADO").ToList();
            var pendientes = sinsaItems.Where(x => x.EstadoPago == "PENDIENTE").ToList();

            resultado.TotalTurnosClientePagado = pagados.Count;
            resultado.TotalHorasOrdClientePagado = Math.Round(pagados.Sum(x => x.HorasOrd), 2);
            resultado.TotalHorasExtClientePagado = Math.Round(pagados.Sum(x => x.HorasExt), 2);
            resultado.TotalHorasClientePagado = resultado.TotalHorasOrdClientePagado + resultado.TotalHorasExtClientePagado;

            resultado.TotalTurnosClientePendiente = pendientes.Count;
            resultado.TotalHorasOrdClientePendiente = Math.Round(pendientes.Sum(x => x.HorasOrd), 2);
            resultado.TotalHorasExtClientePendiente = Math.Round(pendientes.Sum(x => x.HorasExt), 2);
            resultado.TotalHorasClientePendiente = resultado.TotalHorasOrdClientePendiente + resultado.TotalHorasExtClientePendiente;

            resultado.TotalTurnosClienteTotal = sinsaItems.Count;
            resultado.TotalHorasOrdClienteTotal = Math.Round(sinsaItems.Sum(x => x.HorasOrd), 2);
            resultado.TotalHorasExtClienteTotal = Math.Round(sinsaItems.Sum(x => x.HorasExt), 2);
            resultado.TotalHorasClienteTotal = resultado.TotalHorasOrdClienteTotal + resultado.TotalHorasExtClienteTotal;

            // 4. Comparativas y Brechas
            resultado.DiferenciaTurnosGlobal = resultado.TotalTurnosBussersa - resultado.TotalTurnosClienteTotal;
            resultado.DiferenciaHorasOrdGlobal = Math.Round(resultado.TotalHorasOrdBussersa - resultado.TotalHorasOrdClienteTotal, 2);
            resultado.DiferenciaHorasExtGlobal = Math.Round(resultado.TotalHorasExtBussersa - resultado.TotalHorasExtClienteTotal, 2);
            resultado.DiferenciaTotalHorasGlobal = Math.Round(resultado.TotalHorasBussersa - resultado.TotalHorasClienteTotal, 2);

            resultado.BrechaPagadoTurnos = resultado.TotalTurnosBussersa - resultado.TotalTurnosClientePagado;
            resultado.BrechaPagadoHorasOrd = Math.Round(resultado.TotalHorasOrdBussersa - resultado.TotalHorasOrdClientePagado, 2);
            resultado.BrechaPagadoHorasExt = Math.Round(resultado.TotalHorasExtBussersa - resultado.TotalHorasExtClientePagado, 2);
            resultado.BrechaPagadoTotalHoras = Math.Round(resultado.TotalHorasBussersa - resultado.TotalHorasClientePagado, 2);

            // 5. Cruce de Datos Detallado
            var empNormCache = new Dictionary<string, string>();
            string GetNorm(string n)
            {
                if (!empNormCache.TryGetValue(n, out var val))
                {
                    val = NormalizarNombre(n);
                    empNormCache[n] = val;
                }
                return val;
            }

            int numFaltante = 1;
            int numDiferencia = 1;

            var matchBussersaUsado = new HashSet<int>();
            var sinsaItemsUsados = new HashSet<SinsaItem>();

            foreach (var b in bussersaFiltradas)
            {
                var bNorm = GetNorm(b.nombre_empleado);
                var fDate = b.fecha_asistencia.Date;

                // Buscar todos los registros de Sinsa para este colaborador y fecha
                var matchesSinsa = sinsaItems
                    .Where(s => s.Fecha == fDate && CoincideNombre(bNorm, s.EmpleadoNorm))
                    .ToList();

                if (!matchesSinsa.Any())
                {
                    // Turno omitido al 100% por Sinsa
                    string diaSemana = fDate.ToString("dddd", new CultureInfo("es-NI"));
                    diaSemana = char.ToUpper(diaSemana[0]) + diaSemana.Substring(1);

                    resultado.TurnosFaltantes.Add(new TurnoFaltanteClienteDto
                    {
                        No = numFaltante++,
                        IdEmpleado = b.id_empleado,
                        NombreEmpleado = b.nombre_empleado,
                        Fecha = fDate,
                        DiaSemana = diaSemana,
                        Area = b.area_nombre,
                        Cargo = b.cargo ?? "Operador",
                        Turno = b.Turno,
                        HorasOrdCobrables = b.HorasNormalesCobrables,
                        HorasExtCobrables = b.HorasExtrasCobrables,
                        CategoriaFaltante = DeterminarCategoriaFaltante(b.area_nombre, fDate),
                        MotivoJustificacion = $"Turno laborado con biométrico activo en Bussersa ({b.area_nombre}). Omitido en la facturación de Sinsa."
                    });
                }
                else
                {
                    foreach (var m in matchesSinsa) sinsaItemsUsados.Add(m);

                    decimal bTotal = b.HorasNormalesCobrables + b.HorasExtrasCobrables;
                    decimal sOrd = matchesSinsa.Sum(x => x.HorasOrd);
                    decimal sExt = matchesSinsa.Sum(x => x.HorasExt);
                    decimal sTotal = sOrd + sExt;

                    if (Math.Abs(bTotal - sTotal) >= 0.01m || Math.Abs(b.HorasExtrasCobrables - sExt) >= 0.01m)
                    {
                        var primaryMatch = matchesSinsa.First();
                        resultado.DiferenciasDiasComunes.Add(new DiferenciaDiaComunDto
                        {
                            No = numDiferencia++,
                            Fecha = fDate,
                            NombreEmpleado = b.nombre_empleado,
                            Area = b.area_nombre,
                            Cargo = b.cargo ?? primaryMatch.Puesto,
                            HorasOrdBussersa = b.HorasNormalesCobrables,
                            HorasExtBussersa = b.HorasExtrasCobrables,
                            HorasOrdCliente = sOrd,
                            HorasExtCliente = sExt,
                            NoFacturaCliente = string.Join(", ", matchesSinsa.Select(x => x.NoFactura).Where(x => !string.IsNullOrEmpty(x)).Distinct()),
                            EstadoCliente = primaryMatch.EstadoPago,
                            DetalleDiferencia = $"Bussersa (Ord: {b.HorasNormalesCobrables:N2}h, Ext: {b.HorasExtrasCobrables:N2}h) vs Sinsa (Ord: {sOrd:N2}h, Ext: {sExt:N2}h)"
                        });
                    }
                }
            }

            // 6. Facturas en Estado PENDIENTE
            int numPendiente = 1;
            foreach (var p in pendientes)
            {
                resultado.FacturasPendientes.Add(new RegistroPendienteClienteDto
                {
                    No = numPendiente++,
                    Fecha = p.Fecha,
                    NombreEmpleado = p.Empleado,
                    Puesto = p.Puesto,
                    Bodega = p.Bodega,
                    HorasOrd = p.HorasOrd,
                    HorasExt = p.HorasExt,
                    NoFactura = p.NoFactura,
                    EstadoPago = p.EstadoPago,
                    Observacion = "Registrado por Sinsa pero retenido en estado PENDIENTE de pago."
                });
            }

            // 7. Registros Solo en Sinsa (no encontrados en Bussersa)
            int numSoloCliente = 1;
            foreach (var s in sinsaItems)
            {
                if (!sinsaItemsUsados.Contains(s))
                {
                    // Verificar si existe en Bussersa para esa fecha
                    bool exists = bussersaFiltradas.Any(b => b.fecha_asistencia.Date == s.Fecha && CoincideNombre(GetNorm(b.nombre_empleado), s.EmpleadoNorm));
                    if (!exists)
                    {
                        resultado.RegistrosSoloCliente.Add(new RegistroSoloClienteDto
                        {
                            No = numSoloCliente++,
                            Fecha = s.Fecha,
                            NombreEmpleado = s.Empleado,
                            Puesto = s.Puesto,
                            Bodega = s.Bodega,
                            HorasOrd = s.HorasOrd,
                            HorasExt = s.HorasExt,
                            NoFactura = s.NoFactura,
                            EstadoPago = s.EstadoPago,
                            Diagnostico = "Registro presente en Sinsa que no figura en el cierre de marcadas de Bussersa para Sinsa."
                        });
                    }
                }
            }

            // 8. Resumen Consolidado por Colaborador
            int numRes = 1;
            var grupoColaboradores = resultado.TurnosFaltantes
                .GroupBy(x => x.IdEmpleado > 0 ? x.IdEmpleado.ToString() : x.NombreEmpleado)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Sum(x => x.TotalHorasCobrables));

            foreach (var g in grupoColaboradores)
            {
                var first = g.First();
                var fechasList = g.OrderBy(x => x.Fecha).Select(x => x.Fecha.ToString("dd/MM")).Distinct();
                string fechasOmitidas = string.Join(", ", fechasList);

                resultado.ResumenColaboradores.Add(new ResumenColaboradorFaltanteDto
                {
                    No = numRes++,
                    IdEmpleado = first.IdEmpleado,
                    NombreEmpleado = first.NombreEmpleado,
                    AreaPrincipal = first.Area,
                    CargoPrincipal = first.Cargo,
                    DiasOmitidos = g.Count(),
                    HorasOrdOmitidas = Math.Round(g.Sum(x => x.HorasOrdCobrables), 2),
                    HorasExtOmitidas = Math.Round(g.Sum(x => x.HorasExtCobrables), 2),
                    CategoriaFaltante = first.CategoriaFaltante,
                    CausaRaiz = ObtenerCausaRaiz(first.Area, g.Count()),
                    FechasOmitidas = fechasOmitidas
                });
            }

            return resultado;
        }

        private static string NormalizarNombre(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return string.Empty;
            var s = nombre.Trim().ToUpperInvariant();
            s = s.Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U").Replace("Ü", "U");
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        private static bool CoincideNombre(string n1, string n2)
        {
            if (string.Equals(n1, n2, StringComparison.OrdinalIgnoreCase)) return true;
            var t1 = n1.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var t2 = n2.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (t1.Length >= 2 && t2.Length >= 2)
            {
                int coinc = t1.Count(x => t2.Contains(x));
                if (coinc >= 2 && coinc >= Math.Min(t1.Length, t2.Length) - 1)
                    return true;
            }
            return false;
        }

        private static string DeterminarCategoriaFaltante(string area, DateTime fecha)
        {
            if (area.IndexOf("calidad", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Control de Calidad Omitido";
            if (area.IndexOf("logistica", StringComparison.OrdinalIgnoreCase) >= 0 ||
                area.IndexOf("logística", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Logística / Despacho Omitido";
            if (area.IndexOf("ceramica", StringComparison.OrdinalIgnoreCase) >= 0 ||
                area.IndexOf("cerámica", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Cerámica Omitido";
            return "Turno Regular Omitido";
        }

        private static string ObtenerCausaRaiz(string area, int diasOmitidos)
        {
            if (area.IndexOf("calidad", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Sinsa registra esta área como 'Medición y Desempeño' pero omitió cargar gran parte del mes.";
            if (diasOmitidos >= 10)
                return "Colaborador regular con quincena o mes omitido en la consolidación de facturación de Sinsa.";
            return "Día marcado en biométrico de Bussersa pero no reportado en la base de facturación de Sinsa.";
        }
    }
}
