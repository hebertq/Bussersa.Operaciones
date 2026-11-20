using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HostService.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Modelo.Admin;
using Modelo.ClasesGenericas;
using Modelo.Interfaces;
using MudBlazor;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace BsOperaciones.Pages.Operaciones.MovimientosPersonal
{
    public partial class ConsultaAsistenciaMovimientos : ComponentBase
    {
        [Inject] private IOdooService _odooService { get; set; } = null!;
        [Inject] private ISnackbar _snackbar { get; set; } = null!;
        [Inject] private IJSRuntime _js { get; set; } = null!;
        [Inject] private AuthenticationStateProvider _authStateProvider { get; set; } = null!;
        [Inject] private IUserInfo _userInfo { get; set; } = null!;
        [Inject] private NavigationManager _navigation { get; set; } = null!;

        // --- PARÁMETROS Y ESTADO DE FILTROS ---
        protected DateTime? fechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        protected DateTime? fechaHasta = DateTime.Today;
        protected string clienteSeleccionado = "TODOS";
        protected string areaSeleccionada = "TODAS";
        protected string supervisorSeleccionado = "TODOS";
        protected string filtroAsistencia = "TODOS";
        protected string textoBusqueda = string.Empty;
        protected bool estaCargando = false;

        // --- DATOS Y CATÁLOGOS ---
        protected List<MovimientoPersonalDiarioDto> todosMovimientos = new();
        protected List<string> listaClientes = new();
        protected List<string> listaAreas = new();
        protected List<string> listaSupervisores = new();

        // --- FILTRADO EN MEMORIA Y BÚSQUEDA REACTIVA ---
        protected List<MovimientoPersonalDiarioDto> movimientosFiltrados
        {
            get
            {
                var query = todosMovimientos.AsEnumerable();

                // 1. Filtro Condición Asistencia
                if (filtroAsistencia == "PRESENTES")
                {
                    query = query.Where(m => m.Asistio);
                }
                else if (filtroAsistencia == "AUSENCIAS")
                {
                    query = query.Where(m => !m.Asistio);
                }
                else if (filtroAsistencia == "CON_TICKETS")
                {
                    query = query.Where(m => (m.TicketRedmineId.HasValue && m.TicketRedmineId > 0)
                                          || (m.TicketViaticoId.HasValue && m.TicketViaticoId > 0)
                                          || (m.TicketBonoId.HasValue && m.TicketBonoId > 0)
                                          || (m.TicketAusenciaId.HasValue && m.TicketAusenciaId > 0)
                                          || (m.TicketHorasExtrasId.HasValue && m.TicketHorasExtrasId > 0));
                }
                else if (filtroAsistencia == "SIN_TICKETS")
                {
                    query = query.Where(m => (!m.TicketRedmineId.HasValue || m.TicketRedmineId <= 0)
                                          && (!m.TicketViaticoId.HasValue || m.TicketViaticoId <= 0)
                                          && (!m.TicketBonoId.HasValue || m.TicketBonoId <= 0)
                                          && (!m.TicketAusenciaId.HasValue || m.TicketAusenciaId <= 0)
                                          && (!m.TicketHorasExtrasId.HasValue || m.TicketHorasExtrasId <= 0));
                }
                else if (filtroAsistencia == "CON_HORAS_EXTRAS")
                {
                    query = query.Where(m => m.HorasExtras > 0 || (m.TicketHorasExtrasId.HasValue && m.TicketHorasExtrasId > 0));
                }

                // 2. Filtro de Texto Libre (Nombre, Código, Puesto, Observaciones o Tickets)
                if (!string.IsNullOrWhiteSpace(textoBusqueda))
                {
                    string txt = textoBusqueda.Trim().ToLowerInvariant();
                    query = query.Where(m =>
                        (m.NombreEmpleado != null && m.NombreEmpleado.ToLowerInvariant().Contains(txt)) ||
                        m.CodigoEmpleado.ToString().Contains(txt) ||
                        (m.Puesto != null && m.Puesto.ToLowerInvariant().Contains(txt)) ||
                        (m.Area != null && m.Area.ToLowerInvariant().Contains(txt)) ||
                        (m.Cliente != null && m.Cliente.ToLowerInvariant().Contains(txt)) ||
                        (m.UsuarioRegistro != null && m.UsuarioRegistro.ToLowerInvariant().Contains(txt)) ||
                        (m.Observaciones != null && m.Observaciones.ToLowerInvariant().Contains(txt)) ||
                        (m.TicketRedmineId.HasValue && m.TicketRedmineId.Value.ToString().Contains(txt)) ||
                        (m.TicketViaticoId.HasValue && m.TicketViaticoId.Value.ToString().Contains(txt)) ||
                        (m.TicketBonoId.HasValue && m.TicketBonoId.Value.ToString().Contains(txt)) ||
                        (m.TicketAusenciaId.HasValue && m.TicketAusenciaId.Value.ToString().Contains(txt)) ||
                        (m.TicketHorasExtrasId.HasValue && m.TicketHorasExtrasId.Value.ToString().Contains(txt))
                    );
                }

                return query.ToList();
            }
        }

        // --- MÉTRICAS Y KPIS DE AUDITORÍA ---
        protected int TotalColaboradoresUnicos => movimientosFiltrados.Select(m => m.CodigoEmpleado).Distinct().Count();
        protected int TotalAsistieron => movimientosFiltrados.Count(m => m.Asistio);
        protected int TotalAusencias => movimientosFiltrados.Count(m => !m.Asistio);
        protected double PorcentajeAsistencia => movimientosFiltrados.Any() ? (TotalAsistieron * 100.0 / movimientosFiltrados.Count) : 0;
        
        protected decimal TotalHorasEfectivas => (decimal)movimientosFiltrados
            .Where(m => m.Asistio)
            .Sum(m => CalcularHorasEfectivas(m.HoraEntrada, m.HoraSalida, m.TiempoComida));

        protected decimal TotalHorasExtras => movimientosFiltrados
            .Where(m => m.Asistio)
            .Sum(m => m.HorasExtras);

        protected int TotalRegistrosConHe => movimientosFiltrados.Count(m => m.Asistio && m.HorasExtras > 0);

        protected decimal TotalViaticosTrasp => movimientosFiltrados.Sum(m => m.VtcoTrasp);
        protected decimal TotalViaticosAlim => movimientosFiltrados.Sum(m => m.VtcoAlim);
        protected decimal TotalViaticosBnc => movimientosFiltrados.Sum(m => decimal.TryParse(m.Bnc, out var b) ? b : 0);
        protected decimal TotalViaticosGlobal => TotalViaticosTrasp + TotalViaticosAlim + TotalViaticosBnc;

        protected int TotalTicketsCreados => movimientosFiltrados.Count(m =>
            (m.TicketRedmineId.HasValue && m.TicketRedmineId > 0) ||
            (m.TicketViaticoId.HasValue && m.TicketViaticoId > 0) ||
            (m.TicketBonoId.HasValue && m.TicketBonoId > 0) ||
            (m.TicketAusenciaId.HasValue && m.TicketAusenciaId > 0) ||
            (m.TicketHorasExtrasId.HasValue && m.TicketHorasExtrasId > 0)
        );

        // --- CICLO DE VIDA ---
        protected override async Task OnInitializedAsync()
        {
            await CargarCatalogosIniciales();
            await ConsultarMovimientos();
        }

        private async Task CargarCatalogosIniciales()
        {
            try
            {
                var respOper = await _odooService.GetAllCombosHost("Operaciones");
                if (respOper?.Model != null && respOper.Model.Any())
                {
                    listaClientes = respOper.Model.Select(o => o.nombre.Trim()).Distinct().OrderBy(x => x).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar operaciones: {ex.Message}");
            }
        }

        // --- CONSULTA PRINCIPAL AL BACKEND ---
        protected async Task ConsultarMovimientos()
        {
            if (!fechaDesde.HasValue || !fechaHasta.HasValue)
            {
                _snackbar.Add("Debe seleccionar un rango de fechas válido.", Severity.Warning);
                return;
            }

            if (fechaDesde.Value > fechaHasta.Value)
            {
                _snackbar.Add("La fecha de inicio no puede ser posterior a la fecha final.", Severity.Warning);
                return;
            }

            estaCargando = true;
            try
            {
                string? cParam = clienteSeleccionado != "TODOS" ? clienteSeleccionado : null;
                string? aParam = areaSeleccionada != "TODAS" ? areaSeleccionada : null;
                string? uParam = supervisorSeleccionado != "TODOS" ? supervisorSeleccionado : null;

                var resp = await _odooService.ConsultarMovimientosAuditoria(fechaDesde.Value, fechaHasta.Value, cParam, aParam, uParam);
                if (resp?.Model != null)
                {
                    todosMovimientos = resp.Model.ToList();

                    // Alimentar catálogos dinámicamente con los registros devueltos
                    var clientesEncontrados = todosMovimientos.Select(m => m.Cliente).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct();
                    foreach (var c in clientesEncontrados)
                    {
                        if (!listaClientes.Contains(c)) listaClientes.Add(c);
                    }
                    listaClientes.Sort();

                    var supervisoresEncontrados = todosMovimientos.Select(m => m.UsuarioRegistro).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().OrderBy(x => x).ToList();
                    listaSupervisores = supervisoresEncontrados;

                    ActualizarCatalogoAreas();

                    _snackbar.Add($"✅ Se encontraron {todosMovimientos.Count} movimientos en el período consultado.", Severity.Info);
                }
                else
                {
                    todosMovimientos.Clear();
                    _snackbar.Add("No se encontraron registros para los filtros seleccionados.", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al consultar movimientos: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
            }
        }

        protected async Task OnClienteChanged(string nuevoCliente)
        {
            clienteSeleccionado = nuevoCliente;
            ActualizarCatalogoAreas();
            await ConsultarMovimientos();
        }

        private void ActualizarCatalogoAreas()
        {
            if (clienteSeleccionado == "TODOS")
            {
                listaAreas = todosMovimientos
                    .Select(m => m.Area)
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }
            else
            {
                listaAreas = todosMovimientos
                    .Where(m => string.Equals(m.Cliente, clienteSeleccionado, StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.Area)
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }

            if (areaSeleccionada != "TODAS" && !listaAreas.Contains(areaSeleccionada))
            {
                areaSeleccionada = "TODAS";
            }
        }

        // --- RANGOS RÁPIDOS DE FECHA ---
        protected async Task SetRangoRapido(string tipo)
        {
            DateTime hoy = DateTime.Today;
            switch (tipo)
            {
                case "HOY":
                    fechaDesde = hoy;
                    fechaHasta = hoy;
                    break;
                case "AYER":
                    fechaDesde = hoy.AddDays(-1);
                    fechaHasta = hoy.AddDays(-1);
                    break;
                case "SEMANA":
                    int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
                    fechaDesde = hoy.AddDays(-1 * diff);
                    fechaHasta = hoy;
                    break;
                case "QUINCENA":
                    if (hoy.Day <= 15)
                    {
                        fechaDesde = new DateTime(hoy.Year, hoy.Month, 1);
                        fechaHasta = new DateTime(hoy.Year, hoy.Month, 15);
                    }
                    else
                    {
                        fechaDesde = new DateTime(hoy.Year, hoy.Month, 16);
                        fechaHasta = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
                    }
                    break;
                case "MES":
                    fechaDesde = new DateTime(hoy.Year, hoy.Month, 1);
                    fechaHasta = new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));
                    break;
                case "MES_ANTERIOR":
                    var mesAnt = hoy.AddMonths(-1);
                    fechaDesde = new DateTime(mesAnt.Year, mesAnt.Month, 1);
                    fechaHasta = new DateTime(mesAnt.Year, mesAnt.Month, DateTime.DaysInMonth(mesAnt.Year, mesAnt.Month));
                    break;
            }

            await ConsultarMovimientos();
        }

        protected async Task LimpiarFiltros()
        {
            fechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            fechaHasta = DateTime.Today;
            clienteSeleccionado = "TODOS";
            areaSeleccionada = "TODAS";
            supervisorSeleccionado = "TODOS";
            filtroAsistencia = "TODOS";
            textoBusqueda = string.Empty;
            await ConsultarMovimientos();
        }

        // --- CÁLCULO DE HORAS EFECTIVAS ---
        protected double CalcularHorasEfectivas(string horaEntrada, string horaSalida, string? tiempoComida)
        {
            if (string.IsNullOrWhiteSpace(horaEntrada) || string.IsNullOrWhiteSpace(horaSalida))
                return 0;

            if (!TimeSpan.TryParse(horaEntrada, out var entrada) || !TimeSpan.TryParse(horaSalida, out var salida))
                return 0;

            double diffHoras = (salida >= entrada)
                ? (salida - entrada).TotalHours
                : (salida.Add(TimeSpan.FromHours(24)) - entrada).TotalHours;

            double comida = 1.0;
            if (!string.IsNullOrWhiteSpace(tiempoComida) && double.TryParse(tiempoComida, out var cVal))
            {
                comida = cVal;
            }

            double efectivas = diffHoras - comida;
            return efectivas > 0 ? efectivas : 0;
        }

        // --- EXPORTACIÓN ROBUSTA A EXCEL (NPOI) ---
        protected async Task ExportarAExcel()
        {
            if (!movimientosFiltrados.Any())
            {
                _snackbar.Add("No hay movimientos para exportar.", Severity.Warning);
                return;
            }

            try
            {
                var workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("Auditoría Asistencia");

                // 1. Estilos
                var headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerFont.Color = NPOI.HSSF.Util.HSSFColor.White.Index;
                headerFont.FontHeightInPoints = 10;

                var headerStyle = workbook.CreateCellStyle();
                headerStyle.SetFont(headerFont);
                headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.RoyalBlue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;
                headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                headerStyle.VerticalAlignment = VerticalAlignment.Center;

                var titleFont = workbook.CreateFont();
                titleFont.IsBold = true;
                titleFont.FontHeightInPoints = 14;
                titleFont.Color = NPOI.HSSF.Util.HSSFColor.DarkBlue.Index;

                var titleStyle = workbook.CreateCellStyle();
                titleStyle.SetFont(titleFont);

                var currencyFormat = workbook.CreateDataFormat().GetFormat("C$ #,##0.00");
                var currencyStyle = workbook.CreateCellStyle();
                currencyStyle.DataFormat = currencyFormat;

                var numberFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");
                var numberStyle = workbook.CreateCellStyle();
                numberStyle.DataFormat = numberFormat;

                var boldFont = workbook.CreateFont();
                boldFont.IsBold = true;
                var totalStyle = workbook.CreateCellStyle();
                totalStyle.SetFont(boldFont);

                var totalCurrencyStyle = workbook.CreateCellStyle();
                totalCurrencyStyle.SetFont(boldFont);
                totalCurrencyStyle.DataFormat = currencyFormat;

                // 2. Título y Metadatos de Auditoría
                var rowTitle = sheet.CreateRow(0);
                var cellTitle = rowTitle.CreateCell(0);
                cellTitle.SetCellValue("BUSSERSA - REPORTE DE AUDITORÍA Y CONSULTA DE ASISTENCIA");
                cellTitle.CellStyle = titleStyle;

                var rowMeta = sheet.CreateRow(1);
                rowMeta.CreateCell(0).SetCellValue($"Rango: {fechaDesde:dd/MM/yyyy} al {fechaHasta:dd/MM/yyyy} | Cliente: {clienteSeleccionado} | Área: {areaSeleccionada} | Supervisor: {supervisorSeleccionado} | Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}");

                // 3. Fila de Encabezados (Sin ID BD a petición del usuario, incluye todas las columnas operativas)
                string[] headers = new[]
                {
                    "#", "Fecha", "Cliente (Operación)", "Cliente Nómina (Préstamo)", "Área", "Carnet / Código", "Nombre del Colaborador",
                    "Puesto", "Turno", "Asistió", "Entrada", "Salida", "Comida (hrs)", "Horas Efectivas", "Horas Extras (hrs)", "Rango H.E.",
                    "Viático Transporte (C$)", "Viático Alimentación (C$)", "Bono / Nómina (C$)", "Total Viáticos / Bonos (C$)",
                    "Tipo Ausencia", "Días Ausencia", "Ticket Movimiento", "Ticket Viático", "Ticket Bono",
                    "Ticket Ausencia", "Ticket Horas Extras", "Estado H.E.", "Estado", "Registrado Por (Supervisor)", "Fecha Creación", "Observaciones"
                };

                var headerRow = sheet.CreateRow(3);
                headerRow.HeightInPoints = 25;
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // 4. Filas de Datos
                int rowIdx = 4;
                foreach (var m in movimientosFiltrados)
                {
                    var row = sheet.CreateRow(rowIdx);
                    row.CreateCell(0).SetCellValue(rowIdx - 3);
                    row.CreateCell(1).SetCellValue(m.Fecha.ToString("yyyy-MM-dd"));
                    row.CreateCell(2).SetCellValue(m.Cliente);
                    // En cliente nómina solo deben aparecer los que en asistencia estén marcados como préstamos
                    row.CreateCell(3).SetCellValue(m.EsPrestamo ? (m.ClienteOrigenNomina ?? string.Empty) : string.Empty);
                    row.CreateCell(4).SetCellValue(m.Area);
                    row.CreateCell(5).SetCellValue(m.CodigoEmpleado);
                    row.CreateCell(6).SetCellValue(m.NombreEmpleado);
                    row.CreateCell(7).SetCellValue(m.Puesto);
                    row.CreateCell(8).SetCellValue(m.TurnoNombre);
                    row.CreateCell(9).SetCellValue(m.Asistio ? "PRESENTE" : "AUSENTE");
                    row.CreateCell(10).SetCellValue(m.Asistio ? m.HoraEntrada : "--:--");
                    row.CreateCell(11).SetCellValue(m.Asistio ? m.HoraSalida : "--:--");
                    row.CreateCell(12).SetCellValue(m.Asistio ? m.TiempoComida : "0");

                    double horas = m.Asistio ? CalcularHorasEfectivas(m.HoraEntrada, m.HoraSalida, m.TiempoComida) : 0;
                    var cHoras = row.CreateCell(13);
                    cHoras.SetCellValue(horas);
                    cHoras.CellStyle = numberStyle;

                    var cHe = row.CreateCell(14);
                    cHe.SetCellValue(m.Asistio ? (double)m.HorasExtras : 0);
                    cHe.CellStyle = numberStyle;

                    row.CreateCell(15).SetCellValue(m.Asistio && m.HorasExtras > 0 && !string.IsNullOrWhiteSpace(m.HoraInicioExtra)
                        ? $"{m.HoraInicioExtra}-{m.HoraFinExtra}"
                        : "");

                    var cTrasp = row.CreateCell(16);
                    cTrasp.SetCellValue((double)m.VtcoTrasp);
                    cTrasp.CellStyle = currencyStyle;

                    var cAlim = row.CreateCell(17);
                    cAlim.SetCellValue((double)m.VtcoAlim);
                    cAlim.CellStyle = currencyStyle;

                    double bnc = double.TryParse(m.Bnc, out var bVal) ? bVal : 0;
                    var cBnc = row.CreateCell(18);
                    cBnc.SetCellValue(bnc);
                    cBnc.CellStyle = currencyStyle;

                    var cTotV = row.CreateCell(19);
                    cTotV.SetCellValue((double)m.VtcoTrasp + (double)m.VtcoAlim + bnc);
                    cTotV.CellStyle = currencyStyle;

                    row.CreateCell(20).SetCellValue(!m.Asistio ? m.TipoAusencia : "");
                    row.CreateCell(21).SetCellValue(!m.Asistio ? (double)(m.DiasAusencia ?? 1.0m) : 0);
                    row.CreateCell(22).SetCellValue(m.TicketRedmineId.HasValue && m.TicketRedmineId > 0 ? m.TicketRedmineId.Value.ToString() : "");
                    row.CreateCell(23).SetCellValue(m.TicketViaticoId.HasValue && m.TicketViaticoId > 0 ? m.TicketViaticoId.Value.ToString() : "");
                    row.CreateCell(24).SetCellValue(m.TicketBonoId.HasValue && m.TicketBonoId > 0 ? m.TicketBonoId.Value.ToString() : "");
                    row.CreateCell(25).SetCellValue(m.TicketAusenciaId.HasValue && m.TicketAusenciaId > 0 ? m.TicketAusenciaId.Value.ToString() : "");
                    row.CreateCell(26).SetCellValue(m.TicketHorasExtrasId.HasValue && m.TicketHorasExtrasId > 0 ? m.TicketHorasExtrasId.Value.ToString() : "");
                    row.CreateCell(27).SetCellValue(m.EstadoTicketHorasExtras ?? "");
                    row.CreateCell(28).SetCellValue(m.Estado);
                    row.CreateCell(29).SetCellValue(m.UsuarioRegistro ?? "");
                    row.CreateCell(30).SetCellValue(m.FechaCreacion?.ToString("yyyy-MM-dd HH:mm") ?? "");
                    row.CreateCell(31).SetCellValue(m.Observaciones ?? "");

                    rowIdx++;
                }

                // 5. Fila de Totales
                var totalRow = sheet.CreateRow(rowIdx);
                var totalLbl = totalRow.CreateCell(6);
                totalLbl.SetCellValue("TOTALES ACUMULADOS:");
                totalLbl.CellStyle = totalStyle;

                var totalHorasCell = totalRow.CreateCell(13);
                totalHorasCell.SetCellValue((double)TotalHorasEfectivas);
                totalHorasCell.CellStyle = numberStyle;

                var totalHeCell = totalRow.CreateCell(14);
                totalHeCell.SetCellValue((double)TotalHorasExtras);
                totalHeCell.CellStyle = numberStyle;

                var totalTraspCell = totalRow.CreateCell(16);
                totalTraspCell.SetCellValue((double)TotalViaticosTrasp);
                totalTraspCell.CellStyle = totalCurrencyStyle;

                var totalAlimCell = totalRow.CreateCell(17);
                totalAlimCell.SetCellValue((double)TotalViaticosAlim);
                totalAlimCell.CellStyle = totalCurrencyStyle;

                var totalBncCell = totalRow.CreateCell(18);
                totalBncCell.SetCellValue((double)TotalViaticosBnc);
                totalBncCell.CellStyle = totalCurrencyStyle;

                var totalGlobalCell = totalRow.CreateCell(19);
                totalGlobalCell.SetCellValue((double)TotalViaticosGlobal);
                totalGlobalCell.CellStyle = totalCurrencyStyle;

                // 6. Anchos Fijos de Columna Seguros para WebAssembly
                int[] colWidths = new[]
                {
                    6,  // 0: #
                    13, // 1: Fecha
                    18, // 2: Cliente (Operación)
                    24, // 3: Cliente Nómina (Préstamo)
                    22, // 4: Área
                    15, // 5: Carnet / Código
                    32, // 6: Nombre del Colaborador
                    22, // 7: Puesto
                    12, // 8: Turno
                    12, // 9: Asistió
                    10, // 10: Entrada
                    10, // 11: Salida
                    12, // 12: Comida (hrs)
                    14, // 13: Horas Efectivas
                    16, // 14: Horas Extras (hrs)
                    14, // 15: Rango H.E.
                    20, // 16: Viático Transporte (C$)
                    20, // 17: Viático Alimentación (C$)
                    16, // 18: Bono / Nómina (C$)
                    20, // 19: Total Viáticos / Bonos (C$)
                    22, // 20: Tipo Ausencia
                    14, // 21: Días Ausencia
                    18, // 22: Ticket Movimiento
                    18, // 23: Ticket Viático
                    18, // 24: Ticket Bono
                    18, // 25: Ticket Ausencia
                    18, // 26: Ticket Horas Extras
                    16, // 27: Estado H.E.
                    14, // 28: Estado
                    26, // 29: Registrado Por
                    18, // 30: Fecha Creación
                    30  // 31: Observaciones
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    int width = (i < colWidths.Length) ? colWidths[i] : 20;
                    sheet.SetColumnWidth(i, width * 256);
                }

                // 7. Generación de Archivo y Descarga en el Navegador
                using var ms = new MemoryStream();
                workbook.Write(ms);
                byte[] bytes = ms.ToArray();
                string base64 = Convert.ToBase64String(bytes);
                string fileName = $"Auditoria_Asistencia_{fechaDesde:yyyyMMdd}_{fechaHasta:yyyyMMdd}_{clienteSeleccionado.Replace(" ", "_")}.xlsx";

                await _js.InvokeVoidAsync("downloadFile", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", base64, fileName);
                _snackbar.Add($"✅ Archivo '{fileName}' generado exitosamente con {movimientosFiltrados.Count} registros.", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al exportar a Excel: {ex.Message}", Severity.Error);
            }
        }
    }
}
