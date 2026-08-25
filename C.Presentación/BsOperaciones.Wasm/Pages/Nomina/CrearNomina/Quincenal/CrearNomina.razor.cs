using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.Extensions;
using BsOperaciones.Application.Features.Odoo.Commands;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Utilidades.Interfaces;

namespace BsOperaciones.Pages.Nomina.CrearNomina.Quincenal
{
    public partial class CrearNomina : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        public List<diasxpagarperiodo> PayLoadList { get; set; } = new();
        public List<Combos> PayLoadOper { get; set; } = new();
        public DateTime FchaDesde { set; get; } = DateTime.Now.AddDays(-5);
        public DateTime FchaHasta { set; get; } = DateTime.Now;
        public int empresa { set; get; } = 0;

        protected bool isLoading = false;
        protected bool isloaddata = false;
        protected bool fileLoaded = false;
        protected string loadingText = "Cargando...";
        protected string _searchString = "";
        private long maxFileSize = 1024 * 1024 * 15;

        protected DateTime? _fchaDesdeWrapper { get => FchaDesde; set => FchaDesde = value ?? DateTime.Now; }
        protected DateTime? _fchaHastaWrapper { get => FchaHasta; set => FchaHasta = value ?? DateTime.Now; }

        protected override async Task OnInitializedAsync()
        {
            var regop = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
            PayLoadOper = regop.Model;
        }

        protected async Task LoadFiles(InputFileChangeEventArgs e)
        {
            loadingText = "Procesando Excel...";
            isloaddata = true;
            try
            {
                string sFileExtension = Path.GetExtension(e.File.Name).ToLower();
                ISheet sheet;

                using (MemoryStream fs = new MemoryStream())
                {
                    await e.File.OpenReadStream(maxFileSize).CopyToAsync(fs);
                    fs.Position = 0;

                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(fs);
                        sheet = hssfwb.GetSheetAt(0);
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(fs);
                        sheet = hssfwb.GetSheetAt(0);
                    }

                    // Normalizar cabeceras en memoria para tolerar guiones bajos y espacios de forma indistinta en cualquier columna
                    var expectedHeaders = new[] {
                        "id", "nombre", "tipoempleado", "area", "dias habiles", "diastrabajados",
                        "vacdes", "vacpag", "subsidios", "justificados", "injustificados",
                        "cuarentena", "suspension", "septimo", "diasferiados", "totaldias",
                        "hexpagar", "bono", "aguinaldo", "otros ingresos", "otras deducciones"
                    };
                    var headerRow = sheet.GetRow(0);
                    if (headerRow != null)
                    {
                        for (int col = 0; col < headerRow.LastCellNum; col++)
                        {
                            var cell = headerRow.GetCell(col);
                            if (cell != null)
                            {
                                string val = cell.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(val))
                                {
                                    string normalized = val.Replace(" ", "").Replace("_", "").ToLower();
                                    string matchingHeader = expectedHeaders.FirstOrDefault(h => h.Replace(" ", "").Replace("_", "").ToLower() == normalized);
                                    if (matchingHeader != null)
                                    {
                                        cell.SetCellValue(matchingHeader);
                                    }
                                }
                            }
                        }
                    }

                    // Mapeo original de planilla
                    PayLoadList = sheet.MapTo<diasxpagarperiodo>(true, rowMapper =>
                    {
                        return new diasxpagarperiodo()
                        {
                            id = rowMapper.GetValue<int>("id"),
                            nombre = rowMapper.GetValue<string>("nombre"),
                            tipoempleado = rowMapper.GetValue<string>("tipoempleado"),
                            area = rowMapper.GetValue<string>("area"),
                            dias_habiles = rowMapper.GetValue<decimal>("dias habiles"),
                            diastrabajados = rowMapper.GetValue<decimal>("diastrabajados"),
                            vacdes = rowMapper.GetValue<decimal>("vacdes"),
                            vacpag = rowMapper.GetValue<decimal>("vacpag"),
                            subsidios = rowMapper.GetValue<decimal>("subsidios"),
                            justificados = rowMapper.GetValue<decimal>("justificados"),
                            injustificados = rowMapper.GetValue<decimal>("injustificados"),
                            cuarentena = rowMapper.GetValue<decimal>("cuarentena"),
                            suspension = rowMapper.GetValue<decimal>("suspension"),
                            septimo = rowMapper.GetValue<decimal>("septimo"),
                            diasferiados = rowMapper.GetValue<decimal>("diasferiados"),
                            totaldias = rowMapper.GetValue<decimal>("totaldias"),
                            hexpagar = rowMapper.GetValue<decimal>("hexpagar"),
                            bono = rowMapper.GetValue<decimal>("bono"),
                            aguinaldo = rowMapper.GetValue<decimal>("aguinaldo"),
                            otros_ingresos = rowMapper.GetValue<decimal>("otros ingresos"),
                            otras_deducciones = rowMapper.GetValue<decimal>("otras deducciones")
                        };
                    });

                    isLoading = PayLoadList.Any();
                    fileLoaded = true;

                    ActualizarGruposCliente();

                    Snackbar.Add($"{PayLoadList.Count} registros mapeados en {GruposPorCliente.Count} cliente(s) con éxito.", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al leer Excel: " + ex.Message, Severity.Error);
            }
            finally
            {
                isloaddata = false;
                StateHasChanged();
            }
        }

        public class ResumenClienteGroup
        {
            public string NombreCliente { get; set; } = "";
            public int EmpresaId { get; set; }
            public int CantidadEmpleados { get; set; }
            public decimal TotalDias { get; set; }
            public decimal TotalHorasExtras { get; set; }
            public List<diasxpagarperiodo> Empleados { get; set; } = new();
        }

        public List<ResumenClienteGroup> GruposPorCliente { get; set; } = new();

        private void ActualizarGruposCliente()
        {
            if (PayLoadList == null || !PayLoadList.Any())
            {
                GruposPorCliente.Clear();
                return;
            }

            GruposPorCliente = PayLoadList
                .GroupBy(x => string.IsNullOrWhiteSpace(x.area) ? "General" : x.area.Trim())
                .Select(g =>
                {
                    string clienteNombre = g.Key;
                    var matchOper = PayLoadOper?.FirstOrDefault(o =>
                        string.Equals(o.nombre?.Trim(), clienteNombre, StringComparison.OrdinalIgnoreCase) ||
                        (o.nombre != null && o.nombre.Contains(clienteNombre, StringComparison.OrdinalIgnoreCase)));

                    int operId = matchOper?.id ?? 0;

                    return new ResumenClienteGroup
                    {
                        NombreCliente = clienteNombre,
                        EmpresaId = operId,
                        CantidadEmpleados = g.Count(),
                        TotalDias = g.Sum(x => x.totaldias),
                        TotalHorasExtras = g.Sum(x => x.hexpagar),
                        Empleados = g.ToList()
                    };
                }).ToList();
        }

        protected void OnChangeCliente(int value) => empresa = value;

        protected async Task AgregarNominaMasiva()
        {
            if (!GruposPorCliente.Any())
            {
                Snackbar.Add("No hay datos de nómina cargados.", Severity.Warning);
                return;
            }

            loadingText = "Procesando nóminas masivas en Odoo...";
            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);

            int exitosos = 0;
            var errores = new List<string>();

            try
            {
                foreach (var grupo in GruposPorCliente)
                {
                    int empId = grupo.EmpresaId;
                    if (empId == 0)
                    {
                        var match = PayLoadOper?.FirstOrDefault(x => x.nombre.Contains(grupo.NombreCliente, StringComparison.OrdinalIgnoreCase));
                        if (match != null) empId = match.id;
                    }

                    if (empId == 0 && empresa > 0) empId = empresa;

                    string clientenomina = grupo.NombreCliente;
                    string nominanombre = (FchaHasta.Day <= 20 ? "1ra " : "2da ") + "Nómina de " + clientenomina + " " + _Util.AnyoMesLetras(FchaHasta);

                    var nomina = new SolicitarNomina
                    {
                        nombre = nominanombre,
                        cliente = clientenomina,
                        perido = new typeeinout { entrada = FchaDesde, salida = FchaHasta, id = empId },
                        detalle = grupo.Empleados
                    };

                    var response = await _mediator.Send(new CrearNominaComnnad(nomina));
                    if (response.Respuesta.ExisteError)
                    {
                        errores.Add($"{clientenomina}: {response.Respuesta.MensajeError}");
                    }
                    else
                    {
                        exitosos++;
                    }
                }

                if (exitosos > 0)
                {
                    Snackbar.Add($"¡Éxito! Se crearon {exitosos} nómina(s) correctamente en Odoo.", Severity.Success);
                    PayLoadList.Clear();
                    GruposPorCliente.Clear();
                    isLoading = false;
                    fileLoaded = false;
                }

                if (errores.Any())
                {
                    foreach (var err in errores) Snackbar.Add(err, Severity.Error);
                }
            }
            catch (Exception ex) { Snackbar.Add("Fallo en procesamiento masivo: " + ex.Message, Severity.Error); }
            finally { isloaddata = false; StateHasChanged(); }
        }

        protected async Task AgregarNomina()
        {
            await AgregarNominaMasiva();
        }

        protected Func<diasxpagarperiodo, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;
            return x.nombre.Contains(_searchString, StringComparison.OrdinalIgnoreCase) || x.id.ToString().Contains(_searchString);
        };
    }
}
