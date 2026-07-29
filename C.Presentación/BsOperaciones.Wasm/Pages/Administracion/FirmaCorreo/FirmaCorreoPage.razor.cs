using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace BsOperaciones.Wasm.Pages.Administracion.FirmaCorreo
{
    public partial class FirmaCorreoPage : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        public string Nombre { get; set; } = "Ing. Hebert Quintero";
        public string Cargo { get; set; } = "Gerente Administrativo";
        public string Departamento { get; set; } = "Administración";
        public string Telefono { get; set; } = "8654-0495 | 8597-5052";
        public string Email { get; set; } = "info@bussersa.com";
        public string SitioWeb { get; set; } = "www.bussersa.com";
        public string FotoUrl { get; set; } = "https://www.bussersa.com/img/avatars/hebert.png";
        public bool IncluirLeyendaEco { get; set; } = true;
        public bool IncluirConfidencialidad { get; set; } = true;

        private void ResetearValores()
        {
            Nombre = "Ing. Hebert Quintero";
            Cargo = "Gerente Administrativo";
            Departamento = "Administración";
            Telefono = "8654-0495 | 8597-5052";
            Email = "info@bussersa.com";
            SitioWeb = "www.bussersa.com";
            FotoUrl = "https://www.bussersa.com/img/avatars/hebert.png";
            IncluirLeyendaEco = true;
            IncluirConfidencialidad = true;
            Snackbar.Add("Valores restablecidos.", Severity.Info);
        }

        public string ObtenerHtmlFirma()
        {
            string nombreLimpio = string.IsNullOrWhiteSpace(Nombre) ? "Nombre Colaborador" : Nombre.Trim();
            string cargoLimpio = string.IsNullOrWhiteSpace(Cargo) ? "Cargo / Puesto" : Cargo.Trim();
            string deptoLimpio = string.IsNullOrWhiteSpace(Departamento) ? "" : " | " + Departamento.Trim();
            string telLimpio = string.IsNullOrWhiteSpace(Telefono) ? "8654-0495" : Telefono.Trim();
            string emailLimpio = string.IsNullOrWhiteSpace(Email) ? "info@bussersa.com" : Email.Trim();
            string webLimpia = string.IsNullOrWhiteSpace(SitioWeb) ? "www.bussersa.com" : SitioWeb.Trim();
            if (!webLimpia.StartsWith("http://") && !webLimpia.StartsWith("https://"))
            {
                webLimpia = "https://" + webLimpia;
            }

            string fotoHtml = "";
            if (!string.IsNullOrWhiteSpace(FotoUrl))
            {
                fotoHtml = $@"
                <td width=""105"" valign=""top"" style=""padding: 10px 12px 10px 0px; text-align: center;"">
                    <img src=""{FotoUrl.Trim()}"" alt=""{nombreLimpio}"" width=""92"" height=""92"" style=""border-radius: 8px; border: 2px solid #001a33; display: block; object-fit: cover; width: 92px; height: 92px;"" />
                </td>";
            }

            string ecoHtml = "";
            if (IncluirLeyendaEco)
            {
                ecoHtml = @"<p style=""margin: 6px 0 0 0; color: #16a34a; font-size: 10px; font-weight: bold;"">🌱 No me imprimas si no es necesario. Protege el medio ambiente.</p>";
            }

            string confidencialidadHtml = "";
            if (IncluirConfidencialidad)
            {
                confidencialidadHtml = $@"
                <tr>
                    <td colspan=""3"" style=""padding: 8px 12px; border-top: 1px solid #e2e8f0; font-size: 9px; color: #64748b; line-height: 1.35; font-family: Arial, Helvetica, sans-serif;"">
                        El contenido de esta comunicación y de toda documentación anexa es confidencial, dirigido únicamente al destinatario. Si no es el destinatario, le solicitamos que no divulgue su contenido y proceda a su eliminación.
                        {ecoHtml}
                    </td>
                </tr>";
            }

            return $@"
<table cellpadding=""0"" cellspacing=""0"" border=""0"" style=""font-family: Arial, Helvetica, sans-serif; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; width: 560px; max-width: 560px;"">
    <!-- Banner de Encabezado -->
    <tr>
        <td colspan=""3"" style=""background-color: #001a33; color: #D4AF37; padding: 7px 12px; text-align: center; font-size: 13px; font-weight: bold; font-family: Arial, Helvetica, sans-serif;"">
            Soluciones de Personal, Outsourcing y Logística
        </td>
    </tr>
    <tr>
        {fotoHtml}
        <!-- Información del Colaborador -->
        <td valign=""middle"" style=""padding: 10px 8px; font-family: Arial, Helvetica, sans-serif;"">
            <div style=""font-size: 16px; font-weight: bold; color: #001a33; line-height: 1.2;"">{nombreLimpio}</div>
            <div style=""font-size: 11.5px; font-weight: bold; color: #d49537; margin: 3px 0 8px 0; line-height: 1.2;"">{cargoLimpio}{deptoLimpio}</div>
            
            <table cellpadding=""0"" cellspacing=""0"" border=""0"" style=""font-size: 12px; color: #334155; line-height: 1.4; font-family: Arial, Helvetica, sans-serif;"">
                <tr>
                    <td width=""22"" valign=""middle"" style=""padding-bottom: 3px;"">
                        <span style=""display: inline-block; width: 18px; height: 18px; background-color: #001a33; color: #ffffff; border-radius: 4px; text-align: center; line-height: 18px; font-size: 10px; font-weight: bold;"">📞</span>
                    </td>
                    <td valign=""middle"" style=""padding-bottom: 3px; font-weight: bold;"">{telLimpio}</td>
                </tr>
                <tr>
                    <td width=""22"" valign=""middle"" style=""padding-bottom: 3px;"">
                        <span style=""display: inline-block; width: 18px; height: 18px; background-color: #001a33; color: #ffffff; border-radius: 4px; text-align: center; line-height: 18px; font-size: 10px; font-weight: bold;"">✉</span>
                    </td>
                    <td valign=""middle"" style=""padding-bottom: 3px;"">
                        <a href=""mailto:{emailLimpio}"" style=""color: #001a33; text-decoration: none; font-weight: bold;"">{emailLimpio}</a>
                    </td>
                </tr>
                <tr>
                    <td width=""22"" valign=""middle"">
                        <span style=""display: inline-block; width: 18px; height: 18px; background-color: #001a33; color: #ffffff; border-radius: 4px; text-align: center; line-height: 18px; font-size: 10px; font-weight: bold;"">🌐</span>
                    </td>
                    <td valign=""middle"">
                        <a href=""{webLimpia}"" target=""_blank"" style=""color: #001a33; text-decoration: none; font-weight: bold;"">{SitioWeb.Trim()}</a>
                    </td>
                </tr>
            </table>
        </td>
        <!-- Logo Empresa -->
        <td width=""115"" valign=""middle"" style=""padding: 10px 12px; text-align: center;"">
            <a href=""https://www.bussersa.com"" target=""_blank"" style=""text-decoration: none;"">
                <img src=""https://www.bussersa.com/logo.png"" alt=""BUSSERSA"" width=""100"" style=""border: 0; display: block; margin: 0 auto; max-width: 100px;"" />
            </a>
        </td>
    </tr>
    {confidencialidadHtml}
</table>".Trim();
        }

        private async Task CopiarHtml()
        {
            try
            {
                string html = ObtenerHtmlFirma();
                bool exito = await JSRuntime.InvokeAsync<bool>("firmaHelpers.copiarHtml", html);
                if (exito)
                {
                    Snackbar.Add("¡Firma HTML copiada al portapapeles! Ya puedes pegarla en tu correo.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("No se pudo copiar automáticamente. Puedes copiar el código HTML desde la sección inferior.", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al copiar la firma: {ex.Message}", Severity.Error);
            }
        }

        private async Task DescargarImagen()
        {
            try
            {
                string nombreArchivo = $"Firma_BUSSERSA_{Nombre.Replace(" ", "_")}.png";
                bool exito = await JSRuntime.InvokeAsync<bool>("firmaHelpers.descargarImagenFirma", "firmaContainer", nombreArchivo);
                if (exito)
                {
                    Snackbar.Add("¡Imagen de firma descargada con éxito!", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Generando imagen de firma... Si es la primera vez, se copiará el HTML.", Severity.Info);
            }
        }
    }
}
