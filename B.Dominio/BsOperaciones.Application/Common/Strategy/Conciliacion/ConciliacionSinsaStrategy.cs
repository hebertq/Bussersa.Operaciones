using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Modelo.Entidades.Entradas.Odoo;
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

        private class TurnoBussersaInterno
        {
            public int IdEmpleado { get; set; }
            public string NombreEmpleado { get; set; } = string.Empty;
            public string NombreNorm { get; set; } = string.Empty;
            public DateTime Fecha { get; set; }
            public string AreaOriginal { get; set; } = string.Empty;
            public string AreaCanonica { get; set; } = string.Empty;
            public string PuestoOriginal { get; set; } = string.Empty;
            public string PuestoCanonico { get; set; } = string.Empty;
            public decimal HorasOrd { get; set; }
            public decimal HorasExt { get; set; }
            public decimal TotalHoras => HorasOrd + HorasExt;
            public string Turno { get; set; } = string.Empty;
        }

        private class TurnoSinsaInterno
        {
            public DateTime Fecha { get; set; }
            public string Empleado { get; set; } = string.Empty;
            public string EmpleadoNorm { get; set; } = string.Empty;
            public string Codigo { get; set; } = string.Empty;
            public string BodegaOriginal { get; set; } = string.Empty;
            public string AreaCanonica { get; set; } = string.Empty;
            public string PuestoOriginal { get; set; } = string.Empty;
            public string PuestoCanonico { get; set; } = string.Empty;
            public decimal HorasOrd { get; set; }
            public decimal HorasExt { get; set; }
            public decimal TotalHoras => HorasOrd + HorasExt;
        }

        public ResultadoConciliacionDto ConciliarMovimientos(Stream fileStream, List<DiasTrabajadosAreas> movimientosBussersa, DateTime? fchaInicio, DateTime? fchaFin)
        {
            var bussersaInternos = (movimientosBussersa ?? new List<DiasTrabajadosAreas>()).Select(b =>
            {
                string area = !string.IsNullOrWhiteSpace(b.areamov) ? b.areamov : (!string.IsNullOrWhiteSpace(b.area) ? b.area : "General");
                string puesto = !string.IsNullOrWhiteSpace(b.cargo) ? b.cargo : (!string.IsNullOrWhiteSpace(b.tipoempleado) ? b.tipoempleado : "OPERARIO");
                return new TurnoBussersaInterno
                {
                    IdEmpleado = b.id,
                    NombreEmpleado = b.nombre ?? string.Empty,
                    NombreNorm = NormalizarNombre(b.nombre),
                    Fecha = b.fecha.Date,
                    AreaOriginal = area,
                    AreaCanonica = ObtenerAreaCanonica(area),
                    PuestoOriginal = puesto,
                    PuestoCanonico = ObtenerPuestoCanonico(puesto),
                    HorasOrd = b.horas,
                    HorasExt = b.horasextras,
                    Turno = $"{b.entrada} - {b.salida}"
                };
            }).ToList();

            return ProcesarConciliacion(fileStream, bussersaInternos, fchaInicio, fchaFin);
        }

        public ResultadoConciliacionDto Conciliar(Stream fileStream, List<ReporteCierreMarcadasDetalle> marcadasBussersa, DateTime? fchaInicio, DateTime? fchaFin)
        {
            var bussersaInternos = (marcadasBussersa ?? new List<ReporteCierreMarcadasDetalle>()).Select(b =>
            {
                string area = !string.IsNullOrWhiteSpace(b.area_nombre) ? b.area_nombre : "General";
                string puesto = !string.IsNullOrWhiteSpace(b.cargo) ? b.cargo : "OPERARIO";
                return new TurnoBussersaInterno
                {
                    IdEmpleado = b.id_empleado,
                    NombreEmpleado = b.nombre_empleado ?? string.Empty,
                    NombreNorm = NormalizarNombre(b.nombre_empleado),
                    Fecha = b.fecha_asistencia.Date,
                    AreaOriginal = area,
                    AreaCanonica = ObtenerAreaCanonica(area),
                    PuestoOriginal = puesto,
                    PuestoCanonico = ObtenerPuestoCanonico(puesto),
                    HorasOrd = b.HorasNormalesCobrables,
                    HorasExt = b.HorasExtrasCobrables,
                    Turno = b.Turno ?? string.Empty
                };
            }).ToList();

            return ProcesarConciliacion(fileStream, bussersaInternos, fchaInicio, fchaFin);
        }

        private ResultadoConciliacionDto ProcesarConciliacion(Stream fileStream, List<TurnoBussersaInterno> bussersaList, DateTime? fchaInicio, DateTime? fchaFin)
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

            // Identificar índices de columnas dinámicamente
            int colFecha = 4, colEmpleado = 7, colPuesto = 9, colBodega = 1;
            int colHorasOrd = 14, colHorasExt = 15;

            var r0 = table.Rows[0];
            for (int c = 0; c < table.Columns.Count; c++)
            {
                string h = r0[c]?.ToString()?.Trim()?.ToLowerInvariant() ?? "";
                if (h.Contains("empleado") || h.Contains("colaborador") || h.Contains("trabajador")) colEmpleado = c;
                else if (h.Contains("fecha") && !h.Contains("pago") && !h.Contains("salida")) colFecha = c;
                else if ((h.Contains("puesto") || h.Contains("cargo")) && !h.Contains("presupuesto")) colPuesto = c;
                else if (h.Contains("bodega")) colBodega = c;
                else if ((h.Contains("ordinaria") || h.Contains("normal")) && !h.Contains("costo") && !h.Contains("tarifa") && !h.Contains("valor") && !h.Contains("monto")) colHorasOrd = c;
                else if (h.Contains("extra") && !h.Contains("costo") && !h.Contains("tarifa") && !h.Contains("valor") && !h.Contains("monto")) colHorasExt = c;
            }

            var sinsaItems = new List<TurnoSinsaInterno>();
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

                string puesto = row[colPuesto]?.ToString()?.Trim() ?? "";
                string bodega = row[colBodega]?.ToString()?.Trim() ?? "";

                sinsaItems.Add(new TurnoSinsaInterno
                {
                    Fecha = f.Date,
                    Empleado = emp,
                    EmpleadoNorm = NormalizarNombre(emp),
                    BodegaOriginal = bodega,
                    AreaCanonica = ObtenerAreaCanonica(bodega),
                    PuestoOriginal = puesto,
                    PuestoCanonico = ObtenerPuestoCanonico(puesto),
                    HorasOrd = ord,
                    HorasExt = ext
                });
            }

            // Filtrar Bussersa por fechas si aplica
            var bussersaFiltrados = bussersaList;
            if (fchaInicio.HasValue) bussersaFiltrados = bussersaFiltrados.Where(x => x.Fecha >= fchaInicio.Value.Date).ToList();
            if (fchaFin.HasValue) bussersaFiltrados = bussersaFiltrados.Where(x => x.Fecha <= fchaFin.Value.Date).ToList();

            // 2. Totales Generales Bussersa
            resultado.TotalTurnosBussersa = bussersaFiltrados.Count;
            resultado.TotalHorasOrdBussersa = Math.Round(bussersaFiltrados.Sum(x => x.HorasOrd), 2);
            resultado.TotalHorasExtBussersa = Math.Round(bussersaFiltrados.Sum(x => x.HorasExt), 2);
            resultado.TotalHorasBussersa = Math.Round(resultado.TotalHorasOrdBussersa + resultado.TotalHorasExtBussersa, 2);

            // 3. Totales Generales Cliente (Sinsa)
            resultado.TotalTurnosClienteTotal = sinsaItems.Count;
            resultado.TotalHorasOrdClienteTotal = Math.Round(sinsaItems.Sum(x => x.HorasOrd), 2);
            resultado.TotalHorasExtClienteTotal = Math.Round(sinsaItems.Sum(x => x.HorasExt), 2);
            resultado.TotalHorasClienteTotal = Math.Round(resultado.TotalHorasOrdClienteTotal + resultado.TotalHorasExtClienteTotal, 2);

            // 4. CONSOLIDADO POR ÁREA Y PUESTO (EJE PRINCIPAL DE CONCILIACIÓN)
            var grupoBussersa = bussersaFiltrados
                .GroupBy(x => new { Area = x.AreaCanonica, Puesto = x.PuestoCanonico })
                .ToDictionary(g => g.Key, g => new
                {
                    Turnos = g.Count(),
                    Ord = Math.Round(g.Sum(x => x.HorasOrd), 2),
                    Ext = Math.Round(g.Sum(x => x.HorasExt), 2)
                });

            var grupoSinsa = sinsaItems
                .GroupBy(x => new { Area = x.AreaCanonica, Puesto = x.PuestoCanonico })
                .ToDictionary(g => g.Key, g => new
                {
                    Turnos = g.Count(),
                    Ord = Math.Round(g.Sum(x => x.HorasOrd), 2),
                    Ext = Math.Round(g.Sum(x => x.HorasExt), 2)
                });

            var todasLasLlavesAreaPuesto = grupoBussersa.Keys.Union(grupoSinsa.Keys)
                .OrderBy(k => k.Area).ThenBy(k => k.Puesto)
                .ToList();

            int noAP = 1;
            foreach (var key in todasLasLlavesAreaPuesto)
            {
                grupoBussersa.TryGetValue(key, out var bData);
                grupoSinsa.TryGetValue(key, out var sData);

                int turnosB = bData?.Turnos ?? 0;
                int turnosS = sData?.Turnos ?? 0;
                decimal ordB = bData?.Ord ?? 0m;
                decimal ordS = sData?.Ord ?? 0m;
                decimal extB = bData?.Ext ?? 0m;
                decimal extS = sData?.Ext ?? 0m;

                resultado.ConsolidadoAreaPuesto.Add(new ConsolidadoAreaPuestoDto
                {
                    No = noAP++,
                    Area = key.Area,
                    Puesto = key.Puesto,
                    TurnosBussersa = turnosB,
                    TurnosCliente = turnosS,
                    HorasOrdBussersa = ordB,
                    HorasOrdCliente = ordS,
                    HorasExtBussersa = extB,
                    HorasExtCliente = extS
                });
            }

            // 5. CONSOLIDADO SOLO POR ÁREA
            var grupoBussersaArea = bussersaFiltrados
                .GroupBy(x => x.AreaCanonica)
                .ToDictionary(g => g.Key, g => new
                {
                    Turnos = g.Count(),
                    Ord = Math.Round(g.Sum(x => x.HorasOrd), 2),
                    Ext = Math.Round(g.Sum(x => x.HorasExt), 2)
                });

            var grupoSinsaArea = sinsaItems
                .GroupBy(x => x.AreaCanonica)
                .ToDictionary(g => g.Key, g => new
                {
                    Turnos = g.Count(),
                    Ord = Math.Round(g.Sum(x => x.HorasOrd), 2),
                    Ext = Math.Round(g.Sum(x => x.HorasExt), 2)
                });

            var todasLasLlavesSoloArea = grupoBussersaArea.Keys.Union(grupoSinsaArea.Keys)
                .OrderBy(k => k)
                .ToList();

            int noA = 1;
            foreach (var areaKey in todasLasLlavesSoloArea)
            {
                grupoBussersaArea.TryGetValue(areaKey, out var bData);
                grupoSinsaArea.TryGetValue(areaKey, out var sData);

                int turnosB = bData?.Turnos ?? 0;
                int turnosS = sData?.Turnos ?? 0;
                decimal ordB = bData?.Ord ?? 0m;
                decimal ordS = sData?.Ord ?? 0m;
                decimal extB = bData?.Ext ?? 0m;
                decimal extS = sData?.Ext ?? 0m;

                resultado.ConsolidadoSoloArea.Add(new ConsolidadoSoloAreaDto
                {
                    No = noA++,
                    Area = areaKey,
                    TurnosBussersa = turnosB,
                    TurnosCliente = turnosS,
                    HorasOrdBussersa = ordB,
                    HorasOrdCliente = ordS,
                    HorasExtBussersa = extB,
                    HorasExtCliente = extS
                });
            }

            // 6. DETALLE POR COLABORADOR: Cruce Colaborador-Fecha
            var sinsaItemsUsados = new HashSet<TurnoSinsaInterno>();
            int numFaltante = 1;
            int numDiferencia = 1;

            foreach (var b in bussersaFiltrados)
            {
                var matchesSinsa = sinsaItems
                    .Where(s => s.Fecha == b.Fecha && CoincideNombre(b.NombreNorm, s.EmpleadoNorm))
                    .ToList();

                if (!matchesSinsa.Any())
                {
                    string diaSemana = b.Fecha.ToString("dddd", new CultureInfo("es-NI"));
                    diaSemana = char.ToUpper(diaSemana[0]) + diaSemana.Substring(1);

                    resultado.TurnosFaltantes.Add(new TurnoFaltanteClienteDto
                    {
                        No = numFaltante++,
                        IdEmpleado = b.IdEmpleado,
                        NombreEmpleado = b.NombreEmpleado,
                        Fecha = b.Fecha,
                        DiaSemana = diaSemana,
                        Area = b.AreaCanonica,
                        Cargo = b.PuestoCanonico,
                        Turno = b.Turno,
                        HorasOrdCobrables = b.HorasOrd,
                        HorasExtCobrables = b.HorasExt,
                        CategoriaFaltante = DeterminarCategoriaFaltante(b.AreaCanonica, b.Fecha),
                        MotivoJustificacion = $"Turno registrado en Bussersa en el área {b.AreaCanonica} pero no figura en el archivo de Sinsa."
                    });
                }
                else
                {
                    foreach (var m in matchesSinsa) sinsaItemsUsados.Add(m);

                    decimal sOrd = matchesSinsa.Sum(x => x.HorasOrd);
                    decimal sExt = matchesSinsa.Sum(x => x.HorasExt);

                    if (Math.Abs(b.HorasOrd - sOrd) >= 0.01m || Math.Abs(b.HorasExt - sExt) >= 0.01m)
                    {
                        var primaryMatch = matchesSinsa.First();
                        resultado.DiferenciasDiasComunes.Add(new DiferenciaDiaComunDto
                        {
                            No = numDiferencia++,
                            Fecha = b.Fecha,
                            NombreEmpleado = b.NombreEmpleado,
                            AreaBussersa = b.AreaCanonica,
                            BodegaCliente = primaryMatch.AreaCanonica,
                            Cargo = b.PuestoCanonico,
                            HorasOrdBussersa = b.HorasOrd,
                            HorasExtBussersa = b.HorasExt,
                            HorasOrdCliente = sOrd,
                            HorasExtCliente = sExt,
                            DetalleDiferencia = $"Bussersa (Ord: {b.HorasOrd:N2}h, Ext: {b.HorasExt:N2}h) vs Sinsa (Ord: {sOrd:N2}h, Ext: {sExt:N2}h)"
                        });
                    }
                }
            }

            // Registros solo en Sinsa (no encontrados en Bussersa para esa fecha)
            int numSoloCliente = 1;
            foreach (var s in sinsaItems)
            {
                if (!sinsaItemsUsados.Contains(s))
                {
                    bool exists = bussersaFiltrados.Any(b => b.Fecha == s.Fecha && CoincideNombre(b.NombreNorm, s.EmpleadoNorm));
                    if (!exists)
                    {
                        resultado.RegistrosSoloCliente.Add(new RegistroSoloClienteDto
                        {
                            No = numSoloCliente++,
                            Fecha = s.Fecha,
                            NombreEmpleado = s.Empleado,
                            Puesto = s.PuestoCanonico,
                            Bodega = s.AreaCanonica,
                            HorasOrd = s.HorasOrd,
                            HorasExt = s.HorasExt,
                            Diagnostico = $"Registro presente en Sinsa ({s.AreaCanonica}) que no figura en los movimientos de Bussersa."
                        });
                    }
                }
            }

            // Resumen de faltantes por colaborador
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

        public static string NormalizarClave(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim().ToUpperInvariant();
            s = s.Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U").Replace("Ü", "U");
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        public static string ObtenerAreaCanonica(string area)
        {
            var norm = NormalizarClave(area);
            if (string.IsNullOrEmpty(norm)) return "General";

            if (norm.Contains("ALTA TENSION")) return "Bodegon Alta tension";
            if (norm.Contains("CERAMIC")) return "Ceramica";
            if (norm.Contains("CONSTRUC")) return "Construcciones";
            if (norm.Contains("ENVIOS")) return "Despacho envios";
            if (norm.Contains("DESPACHO")) return "Despacho operaciones";
            if (norm.Contains("FERRETERIA") || norm.Contains("ALMACEN C")) return "Ferreteria Almacen C";
            if (norm.Contains("MEDICION") || norm.Contains("DESEMPE") || norm.Contains("CALIDAD")) return "Medición y Desempeño";
            if (norm.Contains("GYPSUM") || norm.Contains("PVC")) return "PVC y Gypsum";
            if (norm.Contains("PROYECT")) return "Proyectos";
            if (norm.Contains("RECEPCION") || norm.Contains("MERCADERIA")) return "Recepcion mercaderia";
            if (norm.Contains("TRANSPORTE") || norm.Contains("DISTRIBUCION")) return "Transporte y Distribucion";
            if (norm.Contains("TRASLADO")) return "Traslados";

            return area.Trim();
        }

        public static string ObtenerPuestoCanonico(string puesto)
        {
            var norm = NormalizarClave(puesto);
            if (string.IsNullOrEmpty(norm)) return "OPERARIO";

            if (norm.Contains("PICKING")) return "OPERARIO PICKING";
            if (norm.Contains("CARGA") || norm.Contains("MONTACARGA") || norm.Contains("EQUIPO")) return "OPERADOR DE EQUIPO DE CARGA";
            if (norm.Contains("DESPACHADOR")) return "DESPACHADOR";
            if (norm.Contains("AUXILIAR")) return "AUXILIAR DE OPERACIONES";
            if (norm.Contains("INGRESO")) return "OPERADOR DE INGRESOS";
            if (norm.Contains("CHOFER") || norm.Contains("CONDUCTOR")) return "CHOFER DE CAMION";
            if (norm.Contains("AYUDANTE")) return "AYUDANTE GENERAL DE OPERACIONES";

            return puesto.Trim().ToUpperInvariant();
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
            if (area.IndexOf("calidad", StringComparison.OrdinalIgnoreCase) >= 0 ||
                area.IndexOf("desempeño", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Control de Calidad / Medición Omitido";
            if (area.IndexOf("logistica", StringComparison.OrdinalIgnoreCase) >= 0 ||
                area.IndexOf("despacho", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Despacho / Operaciones Omitido";
            if (area.IndexOf("ceramica", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Cerámica Omitido";
            return "Turno Regular Omitido";
        }

        private static string ObtenerCausaRaiz(string area, int diasOmitidos)
        {
            if (area.IndexOf("calidad", StringComparison.OrdinalIgnoreCase) >= 0 ||
                area.IndexOf("desempeño", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Sinsa registra esta área como 'Medición y Desempeño' pero omitió cargar fechas del periodo.";
            if (diasOmitidos >= 10)
                return "Colaborador regular con periodo omitido en la planilla de Sinsa.";
            return "Día laborado en Bussersa pero no reportado en la base de Sinsa.";
        }
    }
}
