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
                if (span <= 0m) return 1.00m;

                // Redondear horas ordinarias a bloques limpios (ej. 7.98 -> 8.00, 8.42 -> 8.50, 8.35 -> 8.50, 7.38 -> 7.50 o 8.50)
                decimal diff = span - HorasOrdinarias;
                if (diff <= 0.25m) return 0.00m; // Ej: 09:00-17:00 (8h), 10:00-18:00 (8h), 14:00-21:30 (7.5h)
                if (diff >= 0.75m && diff <= 1.35m) return 1.00m; // Ej: 08:00-17:00 (8h ord + 1h), 08:00-17:30 (8.5h ord + 1h), 07:30-17:00 (8.5h ord + 1h)
                return Math.Round(Math.Max(0m, span - Math.Round(HorasOrdinarias * 2m, MidpointRounding.AwayFromZero) / 2m), 2);
            }
        }

        public string HoraSalidaTurnoConExtras =>
            HorasExtra > 0m ? ConciliacionSinsaHelper.SumarHorasHm(TurnoFin, HorasExtra) : TurnoFin;

        public string HoraInicioExtraSugerida => TurnoFin;

        public string HoraFinExtraSugerida =>
            HorasExtra > 0m ? ConciliacionSinsaHelper.SumarHorasHm(TurnoFin, HorasExtra) : HoraSalidaRelojHm;

        public decimal ExcesoRelojSobreTurnoHoras
        {
            get
            {
                if (!TimeSpan.TryParse(TurnoFin, out var tFinTurno) || !TimeSpan.TryParse(HoraSalidaRelojHm, out var tSalReloj))
                    return 0m;
                double diff = (tSalReloj - tFinTurno).TotalHours;
                if (diff < -12.0) diff += 24.0; // Cruce de medianoche (ej. turno termina 21:00 y reloj sale 01:49)
                return diff >= 0.50 ? Math.Round((decimal)diff, 2) : 0m;
            }
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

        // Estado actual en la pestaña Asistencias
        public bool YaIngresadoEnAsistencia { get; set; }
        public bool AsistioActual { get; set; }
        public string AreaActualAsistencia { get; set; } = string.Empty;
        public string HoraEntradaActual { get; set; } = string.Empty;
        public string HoraSalidaActual { get; set; } = string.Empty;
        public string TiempoComidaActual { get; set; } = "1.00";

        // Elecciones del supervisor en la conciliación
        public bool SeleccionadoAplicar { get; set; } = true;
        public string AreaElegida { get; set; } = string.Empty;
        public string ModoHorarioElegido { get; set; } = "TURNO_CON_HE"; // "TURNO_CON_HE", "MARCACION_RELOJ", "TURNO_PURO", "MANTENER_ACTUAL"
        public decimal HorasExtrasElegidas { get; set; }
        public string HoraInicioExtraElegida { get; set; } = "17:00";
        public string HoraFinExtraElegida { get; set; } = "18:00";

        public bool Vinculado => CodigoEmpleado.HasValue && CodigoEmpleado.Value > 0;

        public bool TieneDiferenciaArea =>
            YaIngresadoEnAsistencia &&
            !string.Equals((AreaActualAsistencia ?? "").Trim(), (Sinsa.Zona ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

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
                    return (Sinsa.TurnoInicio, HorasExtrasElegidas > 0m ? ConciliacionSinsaHelper.SumarHorasHm(Sinsa.TurnoFin, HorasExtrasElegidas) : Sinsa.TurnoFin, comidaSug);
                case "TURNO_CON_HE":
                default:
                    string sal = HorasExtrasElegidas > 0m
                        ? ConciliacionSinsaHelper.SumarHorasHm(Sinsa.TurnoFin, HorasExtrasElegidas)
                        : Sinsa.TurnoFin;
                    return (Sinsa.TurnoInicio, sal, comidaSug);
            }
        }

        public bool TieneDiferenciaHorario
        {
            get
            {
                if (!YaIngresadoEnAsistencia) return false;
                string salEsperada = Sinsa.HorasExtra > 0m ? Sinsa.HoraSalidaTurnoConExtras : Sinsa.TurnoFin;
                bool coincideTurno = string.Equals(HoraEntradaActual, Sinsa.TurnoInicio, StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(HoraSalidaActual, salEsperada, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(HoraSalidaActual, Sinsa.TurnoFin, StringComparison.OrdinalIgnoreCase));
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

            // Detectar fila de encabezados (usualmente fila 1)
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
            List<RegistroArchivoSinsaItem> registrosSinsa,
            List<EmpleadoJornadaItem> empleadosJornadaCliente,
            List<SupervisorPlantillaItemDto> plantillaPersonal,
            List<OdooEmployeeDto> catalogoOdoo,
            List<string> areasCatalogoCliente)
        {
            var resultado = new List<FilaConciliacionSinsaVm>();
            var usadosCodigo = new HashSet<int>();

            foreach (var reg in registrosSinsa)
            {
                string zonaCanonica = ResolverAreaCanonica(reg.Zona, areasCatalogoCliente);

                // 1. Buscar en empleados ya cargados en Asistencias
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

                    decimal heDefault = Math.Round(reg.HorasExtra, 2);
                    string iniExtra = reg.HoraInicioExtraSugerida;
                    string finExtra = heDefault > 0m ? SumarHorasHm(iniExtra, heDefault) : reg.HoraSalidaRelojHm;

                    resultado.Add(new FilaConciliacionSinsaVm
                    {
                        Sinsa = reg,
                        CodigoEmpleado = emp.CodigoEmpleado,
                        IdEmpleado = emp.IdEmpleado > 0 ? emp.IdEmpleado : null,
                        NombreEmpleadoBussersa = emp.NombreEmpleado,
                        PuestoBussersa = string.IsNullOrWhiteSpace(emp.Puesto) ? SugerirPuestoBussersa(reg.PuestoSinsa) : emp.Puesto,
                        YaIngresadoEnAsistencia = true,
                        AsistioActual = emp.Asistio,
                        AreaActualAsistencia = emp.Area ?? string.Empty,
                        HoraEntradaActual = emp.HoraEntrada ?? string.Empty,
                        HoraSalidaActual = emp.HoraSalida ?? string.Empty,
                        TiempoComidaActual = emp.TiempoComida ?? "1.00",
                        SeleccionadoAplicar = true,
                        AreaElegida = zonaCanonica,
                        ModoHorarioElegido = "TURNO_CON_HE",
                        HorasExtrasElegidas = heDefault,
                        HoraInicioExtraElegida = iniExtra,
                        HoraFinExtraElegida = finExtra
                    });
                    continue;
                }

                // 2. Buscar en Mi Plantilla Personal (priorizando SINSA)
                var matchPlantilla = plantillaPersonal
                    .Where(p => p.CodigoEmpleado > 0 && !usadosCodigo.Contains(p.CodigoEmpleado))
                    .Select(p =>
                    {
                        int sc = CalcularPuntajeNombre(reg.NombreEmpleadoSinsa, p.NombreEmpleado);
                        if (sc > 0 && p.ClienteDefault?.Contains("sinsa", StringComparison.OrdinalIgnoreCase) == true)
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
                    decimal heDefault = Math.Round(reg.HorasExtra, 2);
                    string iniExtra = reg.HoraInicioExtraSugerida;
                    string finExtra = heDefault > 0m ? SumarHorasHm(iniExtra, heDefault) : reg.HoraSalidaRelojHm;

                    resultado.Add(new FilaConciliacionSinsaVm
                    {
                        Sinsa = reg,
                        CodigoEmpleado = pl.CodigoEmpleado,
                        IdEmpleado = pl.IdEmpleado > 0 ? pl.IdEmpleado : null,
                        NombreEmpleadoBussersa = pl.NombreEmpleado,
                        PuestoBussersa = string.IsNullOrWhiteSpace(pl.PuestoDefault) ? SugerirPuestoBussersa(reg.PuestoSinsa) : pl.PuestoDefault,
                        YaIngresadoEnAsistencia = false,
                        AsistioActual = false,
                        SeleccionadoAplicar = true,
                        AreaElegida = zonaCanonica,
                        ModoHorarioElegido = "TURNO_CON_HE",
                        HorasExtrasElegidas = heDefault,
                        HoraInicioExtraElegida = iniExtra,
                        HoraFinExtraElegida = finExtra
                    });
                    continue;
                }

                // 3. Buscar en Catálogo General de Odoo
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
                    decimal heDefault = Math.Round(reg.HorasExtra, 2);
                    string iniExtra = reg.HoraInicioExtraSugerida;
                    string finExtra = heDefault > 0m ? SumarHorasHm(iniExtra, heDefault) : reg.HoraSalidaRelojHm;

                    resultado.Add(new FilaConciliacionSinsaVm
                    {
                        Sinsa = reg,
                        CodigoEmpleado = od.CodigoEmpleado.Value,
                        IdEmpleado = od.Id > 0 ? od.Id : null,
                        NombreEmpleadoBussersa = od.Name,
                        PuestoBussersa = SugerirPuestoBussersa(reg.PuestoSinsa),
                        YaIngresadoEnAsistencia = false,
                        AsistioActual = false,
                        SeleccionadoAplicar = true,
                        AreaElegida = zonaCanonica,
                        ModoHorarioElegido = "TURNO_CON_HE",
                        HorasExtrasElegidas = heDefault,
                        HoraInicioExtraElegida = iniExtra,
                        HoraFinExtraElegida = finExtra
                    });
                    continue;
                }

                // 4. Sin coincidencia automática (el supervisor puede vincularlo manualmente en el diálogo)
                resultado.Add(new FilaConciliacionSinsaVm
                {
                    Sinsa = reg,
                    CodigoEmpleado = null,
                    NombreEmpleadoBussersa = string.Empty,
                    PuestoBussersa = SugerirPuestoBussersa(reg.PuestoSinsa),
                    YaIngresadoEnAsistencia = false,
                    SeleccionadoAplicar = false,
                    AreaElegida = zonaCanonica,
                    ModoHorarioElegido = "TURNO_CON_HE",
                    HorasExtrasElegidas = Math.Round(reg.HorasExtra, 2),
                    HoraInicioExtraElegida = reg.HoraInicioExtraSugerida,
                    HoraFinExtraElegida = reg.HorasExtra > 0m ? SumarHorasHm(reg.HoraInicioExtraSugerida, reg.HorasExtra) : reg.HoraSalidaRelojHm
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

            // Si tiene 4 palabras en SINSA y coinciden 3 palabras exactas incluyendo primer nombre y primer apellido
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
                // Redondear al minuto más cercano si trae segundos >= 30 (ej. 17:59:52 -> 18:00)
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

        public static string SumarHorasHm(string? horaBase, decimal horasASumar)
        {
            if (!TimeSpan.TryParse(horaBase, out var tBase))
                tBase = new TimeSpan(17, 0, 0);
            int totalMins = ((int)Math.Round(tBase.TotalMinutes + (double)(horasASumar * 60m), MidpointRounding.AwayFromZero) % 1440 + 1440) % 1440;
            return $"{totalMins / 60:D2}:{totalMins % 60:D2}";
        }
    }
}
