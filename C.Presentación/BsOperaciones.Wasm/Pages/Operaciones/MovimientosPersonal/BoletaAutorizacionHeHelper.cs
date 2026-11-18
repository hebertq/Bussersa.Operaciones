using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Modelo.ClasesGenericas;

namespace BsOperaciones.Pages.Operaciones.MovimientosPersonal
{
    public static class BoletaAutorizacionHeHelper
    {
        public static List<AutorizacionHorasExtrasDto> AgruparPorBoletaArea(IEnumerable<AutorizacionHorasExtrasDto>? items)
        {
            var raw = items?.ToList() ?? new List<AutorizacionHorasExtrasDto>();
            if (!raw.Any()) return new List<AutorizacionHorasExtrasDto>();

            return raw
                .GroupBy(x =>
                    !string.IsNullOrWhiteSpace(x.TokenRecibo)
                        ? $"TOK:{x.TokenRecibo}"
                        : (x.TicketRedmineId.HasValue && x.TicketRedmineId.Value > 0
                            ? $"TCK:{x.TicketRedmineId.Value}"
                            : $"ID:{x.Id}"))
                .Select(g =>
                {
                    var head = g.First();
                    if (head.ColaboradoresGrupo == null || !head.ColaboradoresGrupo.Any())
                    {
                        head.ColaboradoresGrupo = g
                            .OrderBy(f => f.CodigoEmpleado)
                            .Select(f => new ColaboradorAutorizacionItemDto
                            {
                                MovimientoDiarioId = f.MovimientoDiarioId,
                                EmployeeId = f.EmployeeId,
                                CodigoEmpleado = f.CodigoEmpleado,
                                NombreEmpleado = f.NombreEmpleado,
                                Area = f.Area,
                                Cargo = f.Cargo,
                                HoraEntradaJornada = f.HoraEntradaJornada,
                                HoraSalidaJornada = f.HoraSalidaJornada,
                                HoraInicioExtra = f.HoraInicioExtra,
                                HoraFinExtra = f.HoraFinExtra,
                                HorasExtrasAutorizadas = f.HorasExtrasAutorizadas,
                                MotivoIndividual = f.Motivo
                            })
                            .ToList();
                    }
                    return head;
                })
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public static string ConstruirHtmlLoteBoletas(IEnumerable<AutorizacionHorasExtrasDto> items)
        {
            var lista = AgruparPorBoletaArea(items);
            var sb = new StringBuilder();

            for (int i = 0; i < lista.Count; i++)
            {
                var b = lista[i];
                bool pageBreak = i < lista.Count - 1;
                sb.Append(ConstruirHtmlBoletaIndividual(b, pageBreak));
            }

            return sb.ToString();
        }

        public static string ConstruirHtmlBoletaIndividual(AutorizacionHorasExtrasDto b, bool incluirSaltoPagina = false)
        {
            string estadoColor = b.Estado == "RESUELTA" || b.SincronizadoOdoo
                ? "#166534"
                : (b.Estado == "RECHAZADA" ? "#991b1b" : "#92400e");
            string estadoBg = b.Estado == "RESUELTA" || b.SincronizadoOdoo
                ? "#dcfce7"
                : (b.Estado == "RECHAZADA" ? "#fee2e2" : "#fef3c7");
            string estadoTexto = b.Estado == "RESUELTA" || b.SincronizadoOdoo
                ? "AUTORIZADA / APLICADA EN ODOO"
                : (b.Estado == "RECHAZADA" ? "RECHAZADA" : "PENDIENTE APROBACIÓN HELPDESK");

            string ticketLabel = b.TicketRedmineId.HasValue && b.TicketRedmineId.Value > 0
                ? $"Ticket Helpdesk #{b.TicketRedmineId.Value}"
                : $"Registro #{b.Id}";

            var colabs = (b.ColaboradoresGrupo != null && b.ColaboradoresGrupo.Any())
                ? b.ColaboradoresGrupo.OrderBy(c => c.CodigoEmpleado).ToList()
                : new List<ColaboradorAutorizacionItemDto>
                {
                    new ColaboradorAutorizacionItemDto
                    {
                        CodigoEmpleado = b.CodigoEmpleado,
                        NombreEmpleado = b.NombreEmpleado,
                        Area = b.Area,
                        Cargo = b.Cargo,
                        HoraEntradaJornada = b.HoraEntradaJornada,
                        HoraSalidaJornada = b.HoraSalidaJornada
                    }
                };

            string codigosCsv = string.Join(", ", colabs.Select(c => c.CodigoEmpleado));
            var sbColabsRows = new StringBuilder();
            foreach (var c in colabs)
            {
                string jorn = (!string.IsNullOrWhiteSpace(c.HoraEntradaJornada) && !string.IsNullOrWhiteSpace(c.HoraSalidaJornada) && c.HoraEntradaJornada != "--:--")
                    ? $"{WebUtility.HtmlEncode(c.HoraEntradaJornada)} - {WebUtility.HtmlEncode(c.HoraSalidaJornada)}"
                    : "Según marcación";
                sbColabsRows.Append($@"
      <tr>
        <td style=""padding:4px 8px; border:1px solid #cbd5e1; font-family:monospace; font-weight:700; width:90px;"">#{c.CodigoEmpleado}</td>
        <td style=""padding:4px 8px; border:1px solid #cbd5e1; font-weight:600;"">{WebUtility.HtmlEncode(c.NombreEmpleado)}</td>
        <td style=""padding:4px 8px; border:1px solid #cbd5e1;"">{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(c.Cargo) ? b.Cargo : c.Cargo)}</td>
        <td style=""padding:4px 8px; border:1px solid #cbd5e1; text-align:center;"">{jorn}</td>
      </tr>");
            }

            string imgSup = (!string.IsNullOrWhiteSpace(b.FirmaSupervisorBase64) && b.FirmaSupervisorBase64 != "HAS_SIG")
                ? $"<img src=\"{b.FirmaSupervisorBase64}\" alt=\"Firma Supervisor Cliente\" style=\"max-height:74px; max-width:230px; object-fit:contain; display:block; margin:0 auto;\" />"
                : "<div style=\"height:74px; display:flex; align-items:center; justify-content:center; color:#94a3b8; font-size:11px; font-style:italic;\">(Espacio para firma del Supervisor del Cliente)</div>";

            string imgGer = (!string.IsNullOrWhiteSpace(b.FirmaGerenteBase64) && b.FirmaGerenteBase64 != "HAS_SIG")
                ? $"<img src=\"{b.FirmaGerenteBase64}\" alt=\"Firma Gerente Cliente\" style=\"max-height:74px; max-width:230px; object-fit:contain; display:block; margin:0 auto;\" />"
                : "<div style=\"height:74px; display:flex; align-items:center; justify-content:center; color:#94a3b8; font-size:11px; font-style:italic;\">(Espacio para firma del Gerente del Cliente)</div>";

            string supNombre = !string.IsNullOrWhiteSpace(b.SupervisorNombre) ? WebUtility.HtmlEncode(b.SupervisorNombre) : "Supervisor del Cliente";
            string supCargo = !string.IsNullOrWhiteSpace(b.SupervisorCargo) ? WebUtility.HtmlEncode(b.SupervisorCargo) : "Supervisor del Cliente";
            string gerNombre = !string.IsNullOrWhiteSpace(b.GerenteNombre) ? WebUtility.HtmlEncode(b.GerenteNombre) : "Gerente del Cliente";
            string gerCargo = !string.IsNullOrWhiteSpace(b.GerenteCargo) ? WebUtility.HtmlEncode(b.GerenteCargo) : "Gerente del Cliente";

            return $@"
<div class=""boleta-sheet {(incluirSaltoPagina ? "boleta-break" : "")}"">
  <div style=""display:flex; justify-content:space-between; align-items:flex-start; border-bottom:2px solid #013161; padding-bottom:10px; margin-bottom:12px;"">
    <div>
      <div style=""font-size:18px; font-weight:900; color:#013161; letter-spacing:0.6px;"">BUSSERSA &bull; OPERACIONES</div>
      <div style=""font-size:13px; font-weight:700; color:#334155; margin-top:2px;"">BOLETA OFICIAL DE AUTORIZACIÓN DE HORAS EXTRAS POR ÁREA</div>
    </div>
    <div style=""text-align:right;"">
      <div style=""font-size:13px; font-weight:800; color:#013161;"">{ticketLabel}</div>
      <div style=""display:inline-block; margin-top:4px; padding:2px 8px; border-radius:99px; font-size:10.5px; font-weight:800; background:{estadoBg}; color:{estadoColor};"">{estadoTexto}</div>
    </div>
  </div>

  <table style=""width:100%; border-collapse:collapse; font-size:12.5px; margin-bottom:10px;"">
    <tr>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; background:#f8fafc; width:18%; font-weight:700;"">Cliente:</td>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; width:32%; font-weight:700; color:#013161;"">{WebUtility.HtmlEncode(b.ClienteNombre)}</td>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; background:#f8fafc; width:18%; font-weight:700;"">Fecha Laborada:</td>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; width:32%; font-weight:700;"">{b.Fecha:dd/MM/yyyy}</td>
    </tr>
    <tr>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; background:#f8fafc; font-weight:700;"">Área del Cliente:</td>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; font-weight:700;"">{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(b.Area) ? "Operaciones" : b.Area)}</td>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; background:#eff6ff; font-weight:700; color:#1e3a8a;"">Horario H.E.:</td>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; background:#eff6ff; font-weight:800; color:#1e3a8a;"">{WebUtility.HtmlEncode(b.HoraInicioExtra)} a {WebUtility.HtmlEncode(b.HoraFinExtra)} ({b.HorasExtrasAutorizadas:N2} hrs c/u)</td>
    </tr>
    <tr>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; background:#f8fafc; font-weight:700;"">IDs Empleados:</td>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; font-family:monospace; font-weight:700;"" colspan=""3"">{WebUtility.HtmlEncode(codigosCsv)} ({colabs.Count} colaborador{(colabs.Count == 1 ? "" : "es")})</td>
    </tr>
    <tr>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1; background:#f8fafc; font-weight:700;"">Observaciones:</td>
      <td style=""padding:6px 8px; border:1px solid #cbd5e1;"" colspan=""3"">{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(b.Motivo) ? "Autorización de horas extras operativas" : b.Motivo)}</td>
    </tr>
  </table>

  <table style=""width:100%; border-collapse:collapse; font-size:11.5px; margin-bottom:12px;"">
    <thead>
      <tr style=""background:#e2e8f0; color:#0f172a; text-align:left;"">
        <th style=""padding:5px 8px; border:1px solid #cbd5e1;"">Carnet</th>
        <th style=""padding:5px 8px; border:1px solid #cbd5e1;"">Nombre del Colaborador</th>
        <th style=""padding:5px 8px; border:1px solid #cbd5e1;"">Cargo</th>
        <th style=""padding:5px 8px; border:1px solid #cbd5e1; text-align:center;"">Jornada Base</th>
      </tr>
    </thead>
    <tbody>{sbColabsRows}
    </tbody>
  </table>

  <div style=""display:flex; gap:18px; margin-top:14px;"">
    <div style=""flex:1; border:1px solid #cbd5e1; border-radius:8px; padding:8px 12px; text-align:center;"">
      {imgSup}
      <div style=""border-top:1px solid #334155; margin-top:4px; padding-top:4px; font-size:12px; font-weight:700;"">{supNombre}</div>
      <div style=""font-size:10.5px; color:#64748b;"">{supCargo}{(b.FechaFirmaSupervisor.HasValue ? $" &bull; {b.FechaFirmaSupervisor.Value.ToLocalTime():dd/MM/yyyy HH:mm}" : "")}</div>
    </div>
    <div style=""flex:1; border:1px solid #cbd5e1; border-radius:8px; padding:8px 12px; text-align:center;"">
      {imgGer}
      <div style=""border-top:1px solid #334155; margin-top:4px; padding-top:4px; font-size:12px; font-weight:700;"">{gerNombre}</div>
      <div style=""font-size:10.5px; color:#64748b;"">{gerCargo}{(b.FechaFirmaGerente.HasValue ? $" &bull; {b.FechaFirmaGerente.Value.ToLocalTime():dd/MM/yyyy HH:mm}" : "")}</div>
    </div>
  </div>

  <div style=""margin-top:8px; font-size:10px; color:#64748b; display:flex; justify-content:space-between;"">
    <span>Comprobante válido para aplicación en Nómina y Facturación de Servicios (BUSSERSA).</span>
    <span>Emitido: {(b.FechaCreacion?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"))}</span>
  </div>
</div>";
        }
    }
}
