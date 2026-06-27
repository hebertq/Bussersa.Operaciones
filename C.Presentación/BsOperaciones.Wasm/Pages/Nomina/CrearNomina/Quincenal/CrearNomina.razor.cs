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
                    Snackbar.Add($"{PayLoadList.Count} registros mapeados con éxito.", Severity.Success);
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

        protected void OnChangeCliente(int value) => empresa = value;

        protected async Task AgregarNomina()
        {
            if (empresa == 0)
            {
                Snackbar.Add("Seleccione la operación antes de registrar.", Severity.Warning);
                return;
            }

            loadingText = "Enviando a Odoo...";
            isloaddata = true;
            try
            {
                var oper = PayLoadOper.FirstOrDefault(x => x.id == empresa);
                string clientenomina = oper?.nombre ?? "";
                string nominanombre = (FchaHasta.Day <= 20 ? "1ra " : "2da ") + "Nómina de " + clientenomina + " " + _Util.AnyoMesLetras(FchaHasta);

                var nomina = new SolicitarNomina
                {
                    nombre = nominanombre,
                    cliente = clientenomina,
                    perido = new typeeinout { entrada = FchaDesde, salida = FchaHasta, id = empresa },
                    detalle = PayLoadList
                };

                var registros = await _mediator.Send(new CrearNominaComnnad(nomina));
                if (registros.Respuesta.ExisteError) Snackbar.Add(registros.Respuesta.MensajeError, Severity.Error);
                else
                {
                    Snackbar.Add("Nómina enviada correctamente.", Severity.Success);
                    PayLoadList.Clear();
                    isLoading = false;
                    fileLoaded = false;
                }
            }
            catch (Exception ex) { Snackbar.Add("Fallo: " + ex.Message, Severity.Error); }
            finally { isloaddata = false; }
        }

        protected Func<diasxpagarperiodo, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;
            return x.nombre.Contains(_searchString, StringComparison.OrdinalIgnoreCase) || x.id.ToString().Contains(_searchString);
        };
    }
}
