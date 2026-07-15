using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MediatR;
using MudBlazor;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using BsOperaciones.Application.Features.Odoo.Queries;
using BsOperaciones.Application.Features.Odoo.Commands;

namespace BsOperaciones.Pages.Comercial
{
    public partial class ConsultarServicios : ComponentBase
    {
        [Inject] protected IMediator _mediator { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;

        private Combos? selectedTemplate;
        private List<Combos> templatesList = new();
        private List<OdooVariantDto>? variantsList;
        private Dictionary<int, OdooVariantDto> editedVariants = new();

        private bool isSearching = false;
        private bool isSaving = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var response = await _mediator.Send(new GetAllCombosQuery("Plantillas"));
                if (response?.Model != null)
                {
                    templatesList = response.Model.ToList();
                }
                else if (response?.Respuesta != null && response.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Error al cargar plantillas: {response.Respuesta.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al iniciar consulta: {ex.Message}", Severity.Error);
            }
        }

        private async Task<IEnumerable<Combos>> SearchTemplates(string value, CancellationToken token)
        {
            if (string.IsNullOrEmpty(value))
                return templatesList;

            return await Task.FromResult(
                templatesList.Where(x => x.nombre != null && x.nombre.Contains(value, StringComparison.OrdinalIgnoreCase))
            );
        }

        private async Task OnTemplateChanged(Combos? template)
        {
            selectedTemplate = template;
            variantsList = null;
            editedVariants.Clear();

            if (selectedTemplate != null)
            {
                await FetchVariants();
            }
        }

        private async Task FetchVariants()
        {
            if (selectedTemplate == null) return;

            isSearching = true;
            editedVariants.Clear();
            StateHasChanged();

            try
            {
                var response = await _mediator.Send(new GetProductVariantsQuery(selectedTemplate.id));
                if (response?.Model != null)
                {
                    variantsList = response.Model.ToList();
                }
                else if (response?.Respuesta != null && response.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Error al obtener variantes: {response.Respuesta.MensajeError}", Severity.Error);
                    variantsList = new();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al consultar variantes: {ex.Message}", Severity.Error);
                variantsList = new();
            }
            finally
            {
                isSearching = false;
                StateHasChanged();
            }
        }

        private void OnPriceEdited(OdooVariantDto variant, decimal newPrice)
        {
            variant.precio = newPrice;
            if (editedVariants.ContainsKey(variant.id))
            {
                editedVariants[variant.id] = variant;
            }
            else
            {
                editedVariants.Add(variant.id, variant);
            }
        }

        private void DiscardChanges()
        {
            _ = FetchVariants();
        }

        private async Task SaveChanges()
        {
            if (!editedVariants.Any()) return;

            isSaving = true;
            StateHasChanged();

            try
            {
                var response = await _mediator.Send(new ActualizarPreciosVariantesCommand(editedVariants.Values.ToList()));
                if (response != null && !response.Respuesta.ExisteError)
                {
                    Snackbar.Add("Las tarifas de las variantes seleccionadas se han sincronizado con Odoo con éxito.", Severity.Success);
                    await FetchVariants();
                }
                else if (response?.Respuesta != null)
                {
                    Snackbar.Add($"Error al sincronizar tarifas: {response.Respuesta.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al guardar cambios: {ex.Message}", Severity.Error);
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }
    }
}
