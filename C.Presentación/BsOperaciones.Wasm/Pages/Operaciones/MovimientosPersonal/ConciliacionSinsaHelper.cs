using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;

namespace BsOperaciones.Pages.Operaciones.MovimientosPersonal
{
    /// <summary>
    /// Patrón Strategy por Cliente para conciliar archivos externos de asistencia/horas extras
    /// del sistema propio de cada cliente (ej. SINSA gastos_terceros, y futuros clientes).
    /// Para agregar un nuevo cliente en el futuro basta implementar IConciliacionClienteStrategy
    /// y registrarlo en ConciliacionClienteFactory.Estrategias.
    /// </summary>
    public interface IConciliacionClienteStrategy
    {
        string CodigoEstrategia { get; }
        string NombreClienteMostrar { get; }
        string DescripcionFormato { get; }
        string ExtensionesAceptadas { get; }
        bool AplicaACliente(string? nombreCliente, int? operacionId = null);
        List<RegistroArchivoSinsaItem> LeerArchivo(byte[] bytes, string fileName);
        string SugerirPuestoBussersa(string? puestoEnArchivo);
    }

    public static class ConciliacionClienteFactory
    {
        private static readonly List<IConciliacionClienteStrategy> Estrategias = new()
        {
            new ConciliacionSinsaStrategy()
            // Futuros clientes se registran aquí implementando IConciliacionClienteStrategy
        };

        public static IReadOnlyList<IConciliacionClienteStrategy> ObtenerTodas() => Estrategias;

        public static IConciliacionClienteStrategy? ObtenerEstrategia(string? nombreCliente, int? operacionId = null)
        {
            return Estrategias.FirstOrDefault(e => e.AplicaACliente(nombreCliente, operacionId));
        }

        public static bool TieneConciliacionDisponible(string? nombreCliente, int? operacionId = null) =>
            ObtenerEstrategia(nombreCliente, operacionId) != null;
    }

    public class EstadoConciliacionClienteJornada
    {
        public string ClienteNombre { get; set; } = string.Empty;
        public string CodigoEstrategia { get; set; } = string.Empty;
        public string NombreClienteMostrar { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime FechaJornada { get; set; }
        public List<RegistroArchivoSinsaItem> Registros { get; set; } = new();
        public Dictionary<int, RegistroArchivoSinsaItem> RegistrosPorCodigo { get; set; } = new();
    }

    public class ConciliacionSinsaStrategy : IConciliacionClienteStrategy
    {
        public string CodigoEstrategia => "SINSA";
        public string NombreClienteMostrar => "SINSA";
        public string DescripcionFormato => "Reporte gastos_terceros (.xlsx / .csv)";
        public string ExtensionesAceptadas => ".xlsx,.csv";

        public bool AplicaACliente(string? nombreCliente, int? operacionId = null)
        {
            if (operacionId == 23) return true;
            return !string.IsNullOrWhiteSpace(nombreCliente)
                && nombreCliente.Contains("sinsa", StringComparison.OrdinalIgnoreCase);
        }

        public List<RegistroArchivoSinsaItem> LeerArchivo(byte[] bytes, string fileName) =>
            ConciliacionSinsaHelper.LeerArchivoSinsa(bytes, fileName);

        public string SugerirPuestoBussersa(string? puestoEnArchivo) =>
            ConciliacionSinsaHelper.SugerirPuestoBussersa(puestoEnArchivo);
    }

    public class RegistroArchivoSinsaItem
    {
        public int FilaExcel { get; set; }
        public string Zona { get; set; } = string.Empty;
        public DateTime? Fecha { get; set; }
        public string NombreEmpleadoSinsa { get; set; } = string.Empty;
        public string PuestoSinsa { get; set; } = string.Empty;
        public string TurnoTexto { get; set; } = string.Empty;
        public string TurnoInicio { get; set; } = "08:00";
        public string TurnoFin { get; set; } = "17:00";
        public string HoraEntradaRelojRaw { get; set; } = string.Empty;
        public string HoraSalidaRelojRaw { get; set; } = string.Empty;
        public string HoraEntradaRelojHm { get; set; } = "08:00";
        public string HoraSalidaRelojHm { get; set; } = "17:00";
        public decimal HorasOrdinarias { get; set; } = 8.00m;
        public decimal HorasExtra { get; set; } = 0.00m;

        public decimal DuracionBrutaTurnoHoras => ConciliacionSinsaHelper.CalcularDiferenciaHoras(TurnoInicio, TurnoFin);

        public decimal TiempoComidaSugeridoTurno
        {
            get
            {
                decimal span = DuracionBrutaTurnoHoras;
                if (span <= 8.00m) return 0.00m;
                if (span >= 8.75m) return 1.00m;
                return 0.50m;
            }
        }

        public string HoraSalidaTurnoConExtras =>
            HorasExtra > 0m ? ConciliacionSinsaHelper.SumarHorasHm(TurnoFin, HorasExtra) : TurnoFin;

        public string HoraInicioExtraSugerida => TurnoFin;

        public string HoraFinExtraSugerida =>
            HorasExtra > 0m ? ConciliacionSinsaHelper.SumarHorasHm(TurnoFin, HorasExtra) : HoraSalidaRelojHm;

        public decimal ExcesoRelojSobreTurnoHoras =>
            ConciliacionSinsaHelper.CalcularExcesoSalidaSobreTurno(TurnoFin, HoraSalidaRelojHm);

        /// <summary>
        /// Calcula las Horas Extras directamente en base al Turno del día de la persona (TurnoInicio - TurnoFin)
        /// y la fecha de la jornada:
        /// - Si es Domingo (regla SINSA 100% extra): toda la jornada neta trabajada pasa a Horas Extras.
        /// - De Lunes a Sábado: si la hora de salida del turno (TurnoFin) es menor a la hora de salida
        ///   de la marcación (HoraSalidaRelojHm, con tolerancia >= 0.25h / 15 min de reloj), se asigna la
        ///   extra directamente desde TurnoFin hasta HoraSalidaRelojHm (o el valor autorizado en archivo si fuese mayor).
        /// </summary>
        public (decimal HorasExtras, string HoraInicioExtra, string HoraFinExtra, string HoraSalidaJornada, string OrigenTexto) CalcularExtraSegunTurnoYDia(DateTime? fechaJornada = null)
        {
            DateTime fEval = (fechaJornada ?? Fecha ?? DateTime.Today).Date;
            decimal excesoReloj = ExcesoRelojSobreTurnoHoras;

            if (fEval.DayOfWeek == DayOfWeek.Sunday)
            {
                string salDom = excesoReloj >= 0.25m
                    ? HoraSalidaRelojHm
                    : (HorasExtra > 0m ? ConciliacionSinsaHelper.SumarHorasHm(TurnoFin, HorasExtra) : TurnoFin);
                decimal brutasDom = ConciliacionSinsaHelper.CalcularDiferenciaHoras(TurnoInicio, salDom);
                decimal comDom = brutasDom > TiempoComidaSugeridoTurno ? TiempoComidaSugeridoTurno : 0m;
                decimal heDom = Math.Round(Math.Max(0m, brutasDom - comDom), 2);
                if (HorasExtra > heDom)
                {
                    heDom = Math.Round(HorasExtra, 2);
                }
                return (heDom, TurnoInicio, salDom, salDom, "Domingo (100% Extra)");
            }

            if (excesoReloj >= 0.25m)
            {
                if (HorasExtra > excesoReloj)
                {
                    string finAut = ConciliacionSinsaHelper.SumarHorasHm(TurnoFin, HorasExtra);
                    return (Math.Round(HorasExtra, 2), TurnoFin, finAut, finAut, $"Archivo SINSA ({HorasExtra:0.##}h)");
                }
                return (excesoReloj, TurnoFin, HoraSalidaRelojHm, HoraSalidaRelojHm, $"Salida {HoraSalidaRelojHm} > Turno {TurnoFin}");
            }

            if (HorasExtra > 0m)
            {
                string finAut = ConciliacionSinsaHelper.SumarHorasHm(TurnoFin, HorasExtra);
                return (Math.Round(HorasExtra, 2), TurnoFin, finAut, finAut, $"Archivo SINSA ({HorasExtra:0.##}h)");
            }

            return (0m, TurnoFin, TurnoFin, TurnoFin, "Dentro del turno");
        }

        public string TurnoNombreSugerido
        {
            get
            {
                if (TimeSpan.TryParse(TurnoInicio, out var tIni))
                {
                    if (tIni.Hours >= 18) return "Nocturno";
                    if (tIni.Hours >= 13) return "Mixto";
                }
                return "Diurno";
            }
        }
    }

    public class FilaConciliacionSinsaVm
    {
        public RegistroArchivoSinsaItem Sinsa { get; set; } = new();
        public int? CodigoEmpleado { get; set; }
        public int? IdEmpleado { get; set; }
        public string NombreEmpleadoBussersa { get; set; } = string.Empty;
        public string PuestoBussersa { get; set; } = "6-Operador de almacen";

        public bool YaIngresadoEnAsistencia { get; set; }
        public bool AsistioActual { get; set; }
        public string AreaActualAsistencia { get; set; } = string.Empty;
        public string HoraEntradaActual { get; set; } = string.Empty;
        public string HoraSalidaActual { get; set; } = string.Empty;
        public string TiempoComidaActual { get; set; } = "1.00";

        public bool SeleccionadoAplicar { get; set; } = true;
        public string AreaElegida { get; set; } = string.Empty;
        public string ModoHorarioElegido { get; set; } = "TURNO_CON_HE";
        public decimal HorasExtrasElegidas { get; set; }
        public string HoraInicioExtraElegida { get; set; } = "17:00";
        public string HoraFinExtraElegida { get; set; } = "18:00";
        public string OrigenHorasExtras { get; set; } = string.Empty;
        public DateTime FechaJornadaRef { get; set; } = DateTime.Today;

        public bool Vinculado => CodigoEmpleado.HasValue && CodigoEmpleado.Value > 0;

        public bool TieneDiferenciaArea =>
            YaIngresadoEnAsistencia &&
            !string.Equals((AreaActualAsistencia ?? "").Trim(), (Sinsa.Zona ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

        public string ObtenerHoraSalidaConExtrasElegidas()
        {
            if (HorasExtrasElegidas <= 0m)
                return Sinsa.TurnoFin;
            if (!string.IsNullOrWhiteSpace(HoraFinExtraElegida))
                return HoraFinExtraElegida;
            return ConciliacionSinsaHelper.SumarHorasHm(Sinsa.TurnoFin, HorasExtrasElegidas);
        }

        public (string Entrada, string Salida, string Comida) ObtenerHorarioDestino()
        {
            string comidaSug = Sinsa.TiempoComidaSugeridoTurno.ToString("F2", CultureInfo.InvariantCulture);
            switch (ModoHorarioElegido)
            {
                case "MARCACION_RELOJ":
                    return (Sinsa.HoraEntradaRelojHm, Sinsa.HoraSalidaRelojHm, comidaSug);
                case "TURNO_PURO":
                    return (Sinsa.TurnoInicio, Sinsa.TurnoFin, comidaSug);
                case "MANTENER_ACTUAL":
                    if (YaIngresadoEnAsistencia && !string.IsNullOrWhiteSpace(HoraEntradaActual) && HoraEntradaActual != "--:--")
                        return (HoraEntradaActual, HoraSalidaActual, string.IsNullOrWhiteSpace(TiempoComidaActual) ? comidaSug : TiempoComidaActual);
                    return (Sinsa.TurnoInicio, ObtenerHoraSalidaConExtrasElegidas(), comidaSug);
                case "TURNO_CON_HE":
                default:
                    return (Sinsa.TurnoInicio, ObtenerHoraSalidaConExtrasElegidas(), comidaSug);
            }
        }

        public bool TieneDiferenciaHorario
        {
            get
            {
                if (!YaIngresadoEnAsistencia) return false;
                var calcTurno = Sinsa.CalcularExtraSegunTurnoYDia(FechaJornadaRef);
                string salEsperada = calcTurno.HoraSalidaJornada;
                bool coincideTurno = string.Equals(HoraEntradaActual, Sinsa.TurnoInicio, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(HoraSalidaActual, salEsperada, StringComparison.OrdinalIgnoreCase);
                bool coincideReloj = string.Equals(HoraEntradaActual, Sinsa.HoraEntradaRelojHm, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(HoraSalidaActual, Sinsa.HoraSalidaRelojHm, StringComparison.OrdinalIgnoreCase);
                return !coincideTurno && !coincideReloj;
            }
        }
    }

    public class ResultadoDialogConciliarSinsa
    {
        public bool AplicarCambiosMasivos { get; set; }
        public List<FilaConciliacionSinsaVm> Filas { get; set; } = new();
        public List<int> CodigosNoEnSinsaMarcarAusentes { get; set; } = new();
    }

    public static class ConciliacionSinsaHelper
    {
        public static List<RegistroArchivoSinsaItem> LeerArchivoSinsa(byte[] bytes, string fileName)
        {
            if (bytes == null || bytes.Length == 0)
                return new List<RegistroArchivoSinsaItem>();

            if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return LeerCsvSinsa(bytes);

            return LeerXlsxSinsa(bytes);
        }

        private static List<RegistroArchivoSinsaItem> LeerXlsxSinsa(byte[] bytes)
        {
            var resultado = new List<RegistroArchivoSinsaItem>();
            using var ms = new MemoryStream(bytes);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            var ns = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");

            var sharedStrings = new List<string>();
            var ssEntry = zip.GetEntry("xl/sharedStrings.xml");
            if (ssEntry != null)
            {
                using var ssStream = ssEntry.Open();
                var ssDoc = XDocument.Load(ssStream);
                foreach (var si in ssDoc.Descendants(ns + "si"))
                {
                    sharedStrings.Add(string.Concat(si.Descendants(ns + "t").Select(t => t.Value)));
                }
            }

            var sheetEntry = zip.Entries
                .Where(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.FullName)
                .FirstOrDefault();

            if (sheetEntry == null)
                return resultado;

            using var shStream = sheetEntry.Open();
            var shDoc = XDocument.Load(shStream);
            var rows = shDoc.Descendants(ns + "row").ToList();
            if (rows.Count < 2)
                return resultado;

            string LeerCelda(XElement cell)
            {
                string? t = (string?)cell.Attribute("t");
                if (t == "s")
                {
                    string idxStr = cell.Element(ns + "v")?.Value ?? "";
                    if (int.TryParse(idxStr, out int idx) && idx >= 0 && idx < sharedStrings.Count)
                        return sharedStrings[idx]?.Trim() ?? "";
                    return "";
                }
                if (t == "inlineStr")
                {
                    return string.Concat(cell.Descendants(ns + "t").Select(x => x.Value)).Trim();
                }
                return cell.Element(ns + "v")?.Value?.Trim() ?? "";
            }

            static string ExtraerColumnaLetra(string? cellRef)
            {
                if (string.IsNullOrWhiteSpace(cellRef)) return "";
                return new string(cellRef.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
            }

            Dictionary<string, string> mapaColumnas = new(StringComparer.OrdinalIgnoreCase);
            int headerRowIdx = 0;
            for (int i = 0; i < Math.Min(5, rows.Count); i++)
            {
                var tempMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in rows[i].Elements(ns + "c"))
                {
                    string colLet = ExtraerColumnaLetra((string?)c.Attribute("r"));
                    string val = NormalizarTextoBusqueda(LeerCelda(c));
                    if (!string.IsNullOrEmpty(colLet) && !string.IsNullOrEmpty(val))
                        tempMap[colLet] = val;
                }
                if (tempMap.Values.Any(v => v.Contains("EMPLEADO") || v.Contains("ZONA") || v.Contains("TURNO")))
                {
                    mapaColumnas = tempMap;
                    headerRowIdx = i;
                    break;
                }
            }

            string ColPara(string keyword1, string keyword2, string fallbackLetra)
            {
                var hit = mapaColumnas.FirstOrDefault(kv =>
                    kv.Value.Contains(keyword1, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrEmpty(keyword2) || kv.Value.Contains(keyword2, StringComparison.OrdinalIgnoreCase)));
                return !string.IsNullOrEmpty(hit.Key) ? hit.Key : fallbackLetra;
            }

            string colZona = ColPara("ZONA", "", "A");
            if (colZona == "A" && !mapaColumnas.ContainsKey("A")) colZona = ColPara("AREA", "", "A");
            string colFecha = ColPara("FECHA", "", "B");
            string colEmpleado = ColPara("EMPLEADO", "", "C");
            string colPuesto = ColPara("PUESTO", "", "D");
            string colTurno = ColPara("TURNO", "", "E");
            string colEntrada = ColPara("ENTRADA", "", "F");
            string colSalida = ColPara("SALIDA", "", "G");
            string colOrd = ColPara("ORDINARIA", "", "H");
            string colExt = ColPara("EXTRA", "", "I");

            for (int r = headerRowIdx + 1; r < rows.Count; r++)
            {
                var rowEl = rows[r];
                int rowNum = int.TryParse((string?)rowEl.Attribute("r"), out int rn) ? rn : (r + 1);
                var celdasPorLetra = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in rowEl.Elements(ns + "c"))
                {
                    string colLet = ExtraerColumnaLetra((string?)c.Attribute("r"));
                    if (!string.IsNullOrEmpty(colLet))
                        celdasPorLetra[colLet] = LeerCelda(c);
                }

                string GetVal(string col) => celdasPorLetra.TryGetValue(col, out var v) ? v : "";

                string nombre = LimpiarEspacios(GetVal(colEmpleado));
                if (string.IsNullOrWhiteSpace(nombre))
                    continue;

                string zona = LimpiarEspacios(GetVal(colZona));
                string fechaRaw = GetVal(colFecha);
                string puesto = LimpiarEspacios(GetVal(colPuesto));
                string turnoTxt = LimpiarEspacios(GetVal(colTurno));
                string entRaw = LimpiarEspacios(GetVal(colEntrada));
                string salRaw = LimpiarEspacios(GetVal(colSalida));
                string ordRaw = GetVal(colOrd);
                string extRaw = GetVal(colExt);

                var (tIni, tFin) = ParsearRangoTurno(turnoTxt);
                string entHm = ParsearHoraHm(entRaw, tIni);
                string salHm = ParsearHoraHm(salRaw, tFin);

                resultado.Add(new RegistroArchivoSinsaItem
                {
                    FilaExcel = rowNum,
                    Zona = string.IsNullOrWhiteSpace(zona) ? "Operaciones" : zona,
                    Fecha = ParsearFechaExcel(fechaRaw),
                    NombreEmpleadoSinsa = nombre,
                    PuestoSinsa = puesto,
                    TurnoTexto = string.IsNullOrWhiteSpace(turnoTxt) ? $"{tIni} - {tFin}" : turnoTxt,
                    TurnoInicio = tIni,
                    TurnoFin = tFin,
                    HoraEntradaRelojRaw = entRaw,
                    HoraSalidaRelojRaw = salRaw,
                    HoraEntradaRelojHm = entHm,
                    HoraSalidaRelojHm = salHm,
                    HorasOrdinarias = ParsearDecimal(ordRaw, 8.00m),
                    HorasExtra = ParsearDecimal(extRaw, 0.00m)
                });
            }

            return resultado;
        }

        private static List<RegistroArchivoSinsaItem> LeerCsvSinsa(byte[] bytes)
        {
            var resultado = new List<RegistroArchivoSinsaItem>();
            string text = Encoding.UTF8.GetString(bytes);
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return resultado;

            char sep = lines[0].Contains(';') && !lines[0].Contains(',') ? ';' : ',';
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(sep).Select(x => x.Trim().Trim('"')).ToArray();
                if (cols.Length < 5 || string.IsNullOrWhiteSpace(cols[2])) continue;
                var (tIni, tFin) = ParsearRangoTurno(cols[4]);
                resultado.Add(new RegistroArchivoSinsaItem
                {
                    FilaExcel = i + 1,
                    Zona = string.IsNullOrWhiteSpace(cols[0]) ? "Operaciones" : LimpiarEspacios(cols[0]),
                    Fecha = ParsearFechaExcel(cols[1]),
                    NombreEmpleadoSinsa = LimpiarEspacios(cols[2]),
                    PuestoSinsa = LimpiarEspacios(cols[3]),
                    TurnoTexto = LimpiarEspacios(cols[4]),
                    TurnoInicio = tIni,
                    TurnoFin = tFin,
                    HoraEntradaRelojRaw = cols.Length > 5 ? cols[5] : "",
                    HoraSalidaRelojRaw = cols.Length > 6 ? cols[6] : "",
                    HoraEntradaRelojHm = cols.Length > 5 ? ParsearHoraHm(cols[5], tIni) : tIni,
                    HoraSalidaRelojHm = cols.Length > 6 ? ParsearHoraHm(cols[6], tFin) : tFin,
                    HorasOrdinarias = cols.Length > 7 ? ParsearDecimal(cols[7], 8m) : 8m,
                    HorasExtra = cols.Length > 8 ? ParsearDecimal(cols[8], 0m) : 0m
                });
            }
            return resultado;
        }

        public static List<FilaConciliacionSinsaVm> ConstruirFilasConciliacion(
            List<RegistroArchivoSinsaItem> registrosCliente,
            List<EmpleadoJornadaItem> empleadosJornadaCliente,
            List<SupervisorPlantillaItemDto> plantillaPersonal,
            List<OdooEmployeeDto> catalogoOdoo,
            List<string> areasCatalogoCliente,
            IConciliacionClienteStrategy? estrategia = null,
            string? nombreCliente = null,
            DateTime? fechaJornada = null)
        {
            var resultado = new List<FilaConciliacionSinsaVm>();
            var usadosCodigo = new HashSet<int>();
            string filtroCli = nombreCliente?.Trim() ?? "sinsa";

            foreach (var reg in registrosCliente)
            {
                DateTime fechaEval = (fechaJornada ?? reg.Fecha ?? DateTime.Today).Date;
                var calcTurno = reg.CalcularExtraSegunTurnoYDia(fechaEval);
                string zonaCanonica = ResolverAreaCanonica(reg.Zona, areasCatalogoCliente);
                string puestoSug = estrategia != null
                    ? estrategia.SugerirPuestoBussersa(reg.PuestoSinsa)
                    : SugerirPuestoBussersa(reg.PuestoSinsa);

                var matchJornada = empleadosJornadaCliente
                    .Where(e => e.CodigoEmpleado > 0 && !usadosCodigo.Contains(e.CodigoEmpleado))
                    .Select(e => (Emp: e, Score: CalcularPuntajeNombre(reg.NombreEmpleadoSinsa, e.NombreEmpleado)))
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault();

                if (matchJornada.Score > 0 && matchJornada.Emp != null)
                {
                    var emp = matchJornada.Emp;
                    usadosCodigo.Add(emp.CodigoEmpleado);

                    resultado.Add(new FilaConciliacionSinsaVm
                    {
                        Sinsa = reg,
                        FechaJornadaRef = fechaEval,
                        CodigoEmpleado = emp.CodigoEmpleado,
                        IdEmpleado = emp.IdEmpleado > 0 ? emp.IdEmpleado : null,
                        NombreEmpleadoBussersa = emp.NombreEmpleado,
                        PuestoBussersa = string.IsNullOrWhiteSpace(emp.Puesto) ? puestoSug : emp.Puesto,
                        YaIngresadoEnAsistencia = true,
                        AsistioActual = emp.Asistio,
                        AreaActualAsistencia = emp.Area ?? string.Empty,
                        HoraEntradaActual = emp.HoraEntrada ?? string.Empty,
                        HoraSalidaActual = emp.HoraSalida ?? string.Empty,
                        TiempoComidaActual = emp.TiempoComida ?? "1.00",
                        SeleccionadoAplicar = true,
                        AreaElegida = zonaCanonica,
                        ModoHorarioElegido = "TURNO_CON_HE",
                        HorasExtrasElegidas = calcTurno.HorasExtras,
                        HoraInicioExtraElegida = calcTurno.HoraInicioExtra,
                        HoraFinExtraElegida = calcTurno.HoraFinExtra,
                        OrigenHorasExtras = calcTurno.OrigenTexto
                    });
                    continue;
                }

                var matchPlantilla = plantillaPersonal
                    .Where(p => p.CodigoEmpleado > 0 && !usadosCodigo.Contains(p.CodigoEmpleado))
                    .Select(p =>
                    {
                        int sc = CalcularPuntajeNombre(reg.NombreEmpleadoSinsa, p.NombreEmpleado);
                        if (sc > 0 && !string.IsNullOrWhiteSpace(filtroCli) && p.ClienteDefault?.Contains(filtroCli, StringComparison.OrdinalIgnoreCase) == true)
                            sc += 25;
                        return (Pl: p, Score: sc);
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault();

                if (matchPlantilla.Score > 0 && matchPlantilla.Pl != null)
                {
                    var pl = matchPlantilla.Pl;
                    usadosCodigo.Add(pl.CodigoEmpleado);

                    resultado.Add(new FilaConciliacionSinsaVm
                    {
                        Sinsa = reg,
                        FechaJornadaRef = fechaEval,
                        CodigoEmpleado = pl.CodigoEmpleado,
                        IdEmpleado = pl.IdEmpleado > 0 ? pl.IdEmpleado : null,
                        NombreEmpleadoBussersa = pl.NombreEmpleado,
                        PuestoBussersa = string.IsNullOrWhiteSpace(pl.PuestoDefault) ? puestoSug : pl.PuestoDefault,
                        YaIngresadoEnAsistencia = false,
                        AsistioActual = false,
                        SeleccionadoAplicar = true,
                        AreaElegida = zonaCanonica,
                        ModoHorarioElegido = "TURNO_CON_HE",
                        HorasExtrasElegidas = calcTurno.HorasExtras,
                        HoraInicioExtraElegida = calcTurno.HoraInicioExtra,
                        HoraFinExtraElegida = calcTurno.HoraFinExtra,
                        OrigenHorasExtras = calcTurno.OrigenTexto
                    });
                    continue;
                }

                var matchOdoo = catalogoOdoo
                    .Where(o => o.CodigoEmpleado.HasValue && o.CodigoEmpleado.Value > 0 && !usadosCodigo.Contains(o.CodigoEmpleado.Value))
                    .Select(o => (Od: o, Score: CalcularPuntajeNombre(reg.NombreEmpleadoSinsa, o.Name)))
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault();

                if (matchOdoo.Score > 0 && matchOdoo.Od != null)
                {
                    var od = matchOdoo.Od;
                    usadosCodigo.Add(od.CodigoEmpleado!.Value);

                    resultado.Add(new FilaConciliacionSinsaVm
                    {
                        Sinsa = reg,
                        FechaJornadaRef = fechaEval,
                        CodigoEmpleado = od.CodigoEmpleado.Value,
                        IdEmpleado = od.Id > 0 ? od.Id : null,
                        NombreEmpleadoBussersa = od.Name,
                        PuestoBussersa = puestoSug,
                        YaIngresadoEnAsistencia = false,
                        AsistioActual = false,
                        SeleccionadoAplicar = true,
                        AreaElegida = zonaCanonica,
                        ModoHorarioElegido = "TURNO_CON_HE",
                        HorasExtrasElegidas = calcTurno.HorasExtras,
                        HoraInicioExtraElegida = calcTurno.HoraInicioExtra,
                        HoraFinExtraElegida = calcTurno.HoraFinExtra,
                        OrigenHorasExtras = calcTurno.OrigenTexto
                    });
                    continue;
                }

                resultado.Add(new FilaConciliacionSinsaVm
                {
                    Sinsa = reg,
                    FechaJornadaRef = fechaEval,
                    CodigoEmpleado = null,
                    NombreEmpleadoBussersa = string.Empty,
                    PuestoBussersa = puestoSug,
                    YaIngresadoEnAsistencia = false,
                    SeleccionadoAplicar = false,
                    AreaElegida = zonaCanonica,
                    ModoHorarioElegido = "TURNO_CON_HE",
                    HorasExtrasElegidas = calcTurno.HorasExtras,
                    HoraInicioExtraElegida = calcTurno.HoraInicioExtra,
                    HoraFinExtraElegida = calcTurno.HoraFinExtra,
                    OrigenHorasExtras = calcTurno.OrigenTexto
                });
            }

            return resultado;
        }

        public static string SugerirPuestoBussersa(string? puestoSinsa)
        {
            string norm = NormalizarTextoBusqueda(puestoSinsa);
            if (norm.Contains("EQUIPO DE CARGA") || norm.Contains("MONTACARGA"))
                return "10-Operador de montacarga";
            if (norm.Contains("CHOFER") || norm.Contains("CONDUCTOR"))
                return "11-Conductor";
            if (norm.Contains("HOLLER"))
                return "12-Holler";
            return "6-Operador de almacen";
        }

        public static string ResolverAreaCanonica(string? zonaSinsa, List<string>? areasCatalogo)
        {
            if (string.IsNullOrWhiteSpace(zonaSinsa))
                return "Operaciones";
            string zTrim = LimpiarEspacios(zonaSinsa);
            if (areasCatalogo == null || !areasCatalogo.Any())
                return zTrim;

            var exacta = areasCatalogo.FirstOrDefault(a => string.Equals(a?.Trim(), zTrim, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(exacta))
                return exacta.Trim();

            string zNorm = NormalizarTextoBusqueda(zTrim);
            var sinAcentos = areasCatalogo.FirstOrDefault(a => NormalizarTextoBusqueda(a) == zNorm);
            if (!string.IsNullOrWhiteSpace(sinAcentos))
                return sinAcentos.Trim();

            return zTrim;
        }

        public static int CalcularPuntajeNombre(string? nombreSinsa, string? nombreCandidato)
        {
            string nSinsa = NormalizarTextoBusqueda(nombreSinsa);
            string nCand = NormalizarTextoBusqueda(nombreCandidato);
            if (string.IsNullOrEmpty(nSinsa) || string.IsNullOrEmpty(nCand))
                return 0;

            if (nSinsa == nCand)
                return 1000;

            var tSinsa = nSinsa.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tCand = nCand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tSinsa.Length < 2 || tCand.Length < 2)
                return 0;

            int matched = 0;
            foreach (var ts in tSinsa)
            {
                bool hit = tCand.Any(tc =>
                    tc == ts ||
                    (ts.Length >= 4 && tc.Length >= 4 && (tc.StartsWith(ts) || ts.StartsWith(tc) || DistanciaEdicion(ts, tc) <= 1)));
                if (hit) matched++;
            }

            if (matched == tSinsa.Length)
            {
                return 500 + (matched * 50) - Math.Abs(tCand.Length - tSinsa.Length) * 5;
            }

            if (tSinsa.Length >= 4 && matched >= 3 && tCand[0] == tSinsa[0])
            {
                return 350 + (matched * 30);
            }

            return 0;
        }

        private static int DistanciaEdicion(string a, string b)
        {
            int[,] dp = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
                }
            }
            return dp[a.Length, b.Length];
        }

        public static string NormalizarTextoBusqueda(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var formD = s.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var ch in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : ' ');
                }
            }
            return string.Join(" ", sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string LimpiarEspacios(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return string.Join(" ", s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static (string Inicio, string Fin) ParsearRangoTurno(string? turnoTxt)
        {
            if (string.IsNullOrWhiteSpace(turnoTxt))
                return ("08:00", "17:00");

            var parts = turnoTxt.Split(new[] { '-', '–', 'a', 'A' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                string ini = ParsearHoraHm(parts[0].Trim(), "08:00");
                string fin = ParsearHoraHm(parts[1].Trim(), "17:00");
                return (ini, fin);
            }
            return ("08:00", "17:00");
        }

        public static string ParsearHoraHm(string? raw, string fallback = "08:00")
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            string s = raw.Trim();

            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts) && ts.TotalDays < 1.0 && ts.TotalHours >= 0)
            {
                int totalMinutes = (int)Math.Round(ts.TotalMinutes, MidpointRounding.AwayFromZero) % 1440;
                return $"{totalMinutes / 60:D2}:{totalMinutes % 60:D2}";
            }

            if (double.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double oa) && oa >= 0 && oa < 1.0)
            {
                var fromFrac = TimeSpan.FromDays(oa);
                int totalMinutes = (int)Math.Round(fromFrac.TotalMinutes, MidpointRounding.AwayFromZero) % 1440;
                return $"{totalMinutes / 60:D2}:{totalMinutes % 60:D2}";
            }

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                int totalMinutes = (int)Math.Round(dt.TimeOfDay.TotalMinutes, MidpointRounding.AwayFromZero) % 1440;
                return $"{totalMinutes / 60:D2}:{totalMinutes % 60:D2}";
            }

            return fallback;
        }

        private static DateTime? ParsearFechaExcel(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string s = raw.Trim();

            if (DateTime.TryParseExact(s, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
                return dtExact.Date;

            if (double.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double serial) && serial > 40000 && serial < 70000)
                return DateTime.FromOADate(serial).Date;

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtAny))
                return dtAny.Date;

            return null;
        }

        private static decimal ParsearDecimal(string? raw, decimal fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            if (decimal.TryParse(raw.Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
                return Math.Round(val, 2);
            return fallback;
        }

        public static decimal CalcularDiferenciaHoras(string? ini, string? fin)
        {
            if (!TimeSpan.TryParse(ini, out var t1) || !TimeSpan.TryParse(fin, out var t2))
                return 0m;
            if (t1 == t2) return 0m;
            double diff = (t2 - t1).TotalHours;
            if (diff < 0) diff += 24.0;
            return Math.Round((decimal)diff, 2);
        }

        /// <summary>
        /// Calcula el exceso real en horas cuando la hora de salida de la marcación supera la hora de fin del turno del día
        /// (con tolerancia estándar de reloj biométrico >= 0.25h / 15 min), soportando salidas de madrugada post-turno.
        /// </summary>
        public static decimal CalcularExcesoSalidaSobreTurno(string? turnoFin, string? horaSalidaMarcacion)
        {
            if (!TimeSpan.TryParse(turnoFin, out var tFinTurno) || !TimeSpan.TryParse(horaSalidaMarcacion, out var tSalReloj))
                return 0m;

            double diff = (tSalReloj - tFinTurno).TotalHours;
            if (diff < -12.0) diff += 24.0;
            if (diff < 0.25 || diff > 14.0)
                return 0m;

            return Math.Round((decimal)diff, 2);
        }

        public static string SumarHorasHm(string? horaBase, decimal horasASumar)
        {
            if (!TimeSpan.TryParse(horaBase, out var tBase))
                tBase = new TimeSpan(17, 0, 0);
            int totalMins = ((int)Math.Round(tBase.TotalMinutes + (double)(horasASumar * 60m), MidpointRounding.AwayFromZero) % 1440 + 1440) % 1440;
            return $"{totalMins / 60:D2}:{totalMins % 60:D2}";
        }
    }
}
