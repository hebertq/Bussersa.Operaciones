using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MediatR;
using BsOperaciones.Application.Features.Comercial.Queries;

namespace BsOperaciones.Pages.Rrhh
{
    public partial class MatrizDescriptores : ComponentBase
    {
        [Inject] private IMediator Mediator { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IJSRuntime JS { get; set; }

        public List<JobDescription> JobDescriptionsList { get; set; } = new();
        public List<string> SelectedJobTitles { get; set; } = new();
        public bool isPrinting = false;

        protected override void OnInitialized()
        {
            LoadJobDescriptions();
            // Select all by default
            SelectedJobTitles = JobDescriptionsList.Select(j => j.Title).ToList();
        }

        private void ToggleJobSelection(string title)
        {
            if (SelectedJobTitles.Contains(title))
            {
                if (SelectedJobTitles.Count > 1) // Keep at least one selected
                {
                    SelectedJobTitles.Remove(title);
                }
                else
                {
                    Snackbar.Add("Debe seleccionar al menos un puesto para comparar.", Severity.Warning);
                }
            }
            else
            {
                SelectedJobTitles.Add(title);
            }
            StateHasChanged();
        }

        public async Task PrintMatrixPdf()
        {
            isPrinting = true;
            StateHasChanged();
            try
            {
                var res = await Mediator.Send(new PrintMatrizDescriptoresPdfQuery());
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", "Matriz_Descriptores_Puestos.pdf", fileBytes, "application/pdf");
                    Snackbar.Add("Matriz de descriptores generada y descargada.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar PDF: " + res.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error inesperado: " + ex.Message, Severity.Error);
            }
            finally
            {
                isPrinting = false;
                StateHasChanged();
            }
        }

        public async Task PrintJobPdf(string jobTitle)
        {
            isPrinting = true;
            StateHasChanged();
            try
            {
                var res = await Mediator.Send(new PrintDescriptorPdfQuery(jobTitle));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", $"Descriptor_{jobTitle.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
                    Snackbar.Add($"Descriptor de {jobTitle} generado y descargado.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar PDF: " + res.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error inesperado: " + ex.Message, Severity.Error);
            }
            finally
            {
                isPrinting = false;
                StateHasChanged();
            }
        }

        private void LoadJobDescriptions()
        {
            // Reuse identical job descriptions structure
            JobDescriptionsList = new List<JobDescription>
            {
                new JobDescription
                {
                    Title = "Operario(a) de Empaque",
                    TabIcon = Icons.Material.Filled.Inventory2,
                    Department = "Operaciones",
                    ReportsTo = "Supervisor(a) de Turno",
                    Supervises = "No aplica",
                    Shift = "Día o Noche (según turno asignado)",
                    EmploymentType = "Planilla directa (BUSSERSA)",
                    Objective = "Empacar el producto terminado cumpliendo los estándares de calidad, peso y presentación establecidos, reportando oportunamente cualquier falla al/a la Supervisor(a) de Turno.",
                    EssentialFunctions = new List<string>
                    {
                        "Empacar el producto terminado según estándar y ritmo de línea.",
                        "Verificar peso, sellado y presentación del empaque antes de paletizar.",
                        "Reportar fallas de máquina o de materia prima al/a la Supervisor(a) de Turno."
                    },
                    OccasionalFunctions = new List<string>
                    {
                        "Apoyar labores de limpieza y orden de su estación de trabajo (5S).",
                        "Apoyar al área de etiquetado en picos de producción."
                    },
                    Education = "Educación primaria / secundaria (según requisito interno).",
                    Experience = "No indispensable; deseable experiencia previa en líneas de producción/empaque.",
                    TechnicalKnowledge = "Manejo básico de estándares de empaque; deseable experiencia en manufactura de alimentos/bebidas.",
                    ToolsLanguages = "Español (nativo).",
                    Competencies = new List<string>
                    {
                        "Atención al detalle",
                        "Ritmo de trabajo constante",
                        "Trabajo en equipo",
                        "Disciplina y puntualidad",
                        "Disposición para trabajar de pie y turnos rotativos"
                    },
                    Horary = "Turno completo (día u noche) según rol de turnos de la planta.",
                    EppRequirements = "Cofia, tapones auditivos, calzado de seguridad, uniforme según normativa de la planta.",
                    Risks = "Movimientos repetitivos; exposición a ruido de maquinaria; manejo de cargas ligeras.",
                    Kpis = new List<string>
                    {
                        "Cumplimiento de la meta de unidades empacadas por turno.",
                        "Porcentaje de producto rechazado por defecto de empaque ≤ meta."
                    }
                },
                new JobDescription
                {
                    Title = "Operario(a) de Etiquetado",
                    TabIcon = Icons.Material.Filled.Label,
                    Department = "Operaciones",
                    ReportsTo = "Supervisor(a) de Turno",
                    Supervises = "No aplica",
                    Shift = "Día o Noche (según turno asignado)",
                    EmploymentType = "Planilla directa (BUSSERSA)",
                    Objective = "Etiquetar el producto conforme al tipo de etiqueta y turno asignado, verificando legibilidad y trazabilidad (lote y fecha de vencimiento) en cada unidad.",
                    EssentialFunctions = new List<string>
                    {
                        "Etiquetar el producto según el tipo de etiqueta y turno asignado.",
                        "Verificar legibilidad, lote y fecha de vencimiento en cada etiqueta.",
                        "Registrar el consumo de etiquetas y reportar faltantes."
                    },
                    OccasionalFunctions = new List<string>
                    {
                        "Apoyar labores de limpieza y orden de su estación de trabajo (5S).",
                        "Apoyar al área de empaque en picos de producción."
                    },
                    Education = "Educación primaria / secundaria (según requisito interno).",
                    Experience = "No indispensable; deseable experiencia previa en líneas de producción/etiquetado.",
                    TechnicalKnowledge = "Manejo básico de estándares de etiquetado y trazabilidad de lote.",
                    ToolsLanguages = "Español (nativo).",
                    Competencies = new List<string>
                    {
                        "Atención al detalle",
                        "Precisión visual",
                        "Trabajo en equipo",
                        "Disciplina y puntualidad",
                        "Disposición para trabajar de pie y turnos rotativos"
                    },
                    Horary = "Turno completo (día u noche) según rol de turnos de la planta.",
                    EppRequirements = "Cofia, tapones auditivos, calzado de seguridad, uniforme según normativa de la planta.",
                    Risks = "Movimientos repetitivos; exposición a ruido de maquinaria.",
                    Kpis = new List<string>
                    {
                        "Cumplimiento de la meta de unidades etiquetadas por turno.",
                        "Porcentaje de producto rechazado por error de etiquetado ≤ meta."
                    }
                },
                new JobDescription
                {
                    Title = "Auxiliar de Bodega y Logística",
                    TabIcon = Icons.Material.Filled.Store,
                    Department = "Operaciones",
                    ReportsTo = "Supervisor(a) de Turno",
                    Supervises = "No aplica",
                    Shift = "Día o Noche (según turno asignado)",
                    EmploymentType = "Planilla directa (BUSSERSA)",
                    Objective = "Recibir, controlar y despachar materia prima, insumos y producto terminado, garantizando el abastecimiento continuo de la línea y la trazabilidad del inventario de bodega.",
                    EssentialFunctions = new List<string>
                    {
                        "Recibir, verificar y almacenar materia prima e insumos de empaque/etiquetado.",
                        "Controlar el inventario de insumos (etiquetas, cajas, film) y alertar reórdenes.",
                        "Despachar producto terminado según orden de carga y coordinar con transporte."
                    },
                    OccasionalFunctions = new List<string>
                    {
                        "Apoyar conteos físicos periódicos de inventario junto al/a la Supervisor(a) de Turno.",
                        "Apoyar labores de orden y limpieza de bodega (5S)."
                    },
                    Education = "Educación primaria / secundaria (según requisito interno).",
                    Experience = "6 meses a 1 año en bodega, almacén o logística.",
                    TechnicalKnowledge = "Manejo básico de inventarios, uso de montacargas/transpaleta (si aplica), control de trazabilidad de lote.",
                    ToolsLanguages = "Español (nativo).",
                    Competencies = new List<string>
                    {
                        "Orden y control de inventario",
                        "Responsabilidad en manejo de insumos",
                        "Trabajo en equipo con producción",
                        "Disposición para trabajar en turnos rotativos"
                    },
                    Horary = "Turno completo (día u noche) según rol de turnos de la planta.",
                    EppRequirements = "Calzado de seguridad, guantes, chaleco reflectivo según normativa de bodega.",
                    Risks = "Manejo de cargas; riesgo de golpes o caídas propios de bodega.",
                    Kpis = new List<string>
                    {
                        "0% de desabastecimiento de insumos críticos en línea.",
                        "Exactitud de inventario de bodega ≥ meta establecida."
                    }
                }
            };
        }
    }
}
