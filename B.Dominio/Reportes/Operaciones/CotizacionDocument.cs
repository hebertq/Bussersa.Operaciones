using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Modelo.ClasesGenericas;
using Modelo.Comercial;
using Modelo.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Componentes;

namespace Reportes.Operaciones
{
    public class CotizacionDocument
    {
        public CotizacionDocument() { }

        public ISingleResponse<FileNameString> PrintPdf(string clienteNombre, List<Cotizacion> cotizaciones)
        {
            ISingleResponse<FileNameString> response = new SingleResponse<FileNameString>();
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var pdfdocument = GenerateReport(clienteNombre, cotizaciones).GeneratePdf();
                response.Model.File = Convert.ToBase64String(pdfdocument.ToArray());
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Servicio, ex, "CotizacionDocument");
            }

            return response;
        }

        private static Document GenerateReport(string clienteNombre, List<Cotizacion> cotizaciones)
        {
            var _Header = new HeaderComponent("COTIZACIÓN DE SERVICIOS", clienteNombre, "Fecha de Generación: ", DateTime.Now.ToString("dd/MM/yyyy"));

            IContainer DefaultCellStyle(IContainer container)
            {
                return container
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignCenter()
                    .AlignMiddle();
            }

            IContainer DefaultHeaderStyle(IContainer container, string backgroundColor)
            {
                return container
                    .Border(1)
                    .BorderColor(Colors.Grey.Darken1)
                    .Background(backgroundColor)
                    .Padding(6)
                    .AlignCenter()
                    .AlignMiddle();
            }

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.Header().Component(_Header);

                    page.Content().Column(column =>
                    {
                        column.Item().PaddingTop(15).Text("Resumen de Propuesta por Turno").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                        column.Item().PaddingTop(5).Text("A continuación se detallan los costos asociados a cada turno cotizado para cubrir el servicio requerido:").FontSize(10);

                        // Tabla resumen de turnos
                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Turno
                                columns.RelativeColumn(1.5f); // Horas Turno
                                columns.RelativeColumn(2); // Salario Base
                                columns.RelativeColumn(2); // Viáticos
                                columns.RelativeColumn(2); // EPP
                                columns.RelativeColumn(2.5f); // Tarifa Acordada / Mes
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Turno").FontSize(9).Bold();
                                header.Cell().Element(CellStyle).Text("Horas").FontSize(9).Bold();
                                header.Cell().Element(CellStyle).Text("Salario Base").FontSize(9).Bold();
                                header.Cell().Element(CellStyle).Text("Viáticos/Mes").FontSize(9).Bold();
                                header.Cell().Element(CellStyle).Text("EPP/Mes").FontSize(9).Bold();
                                header.Cell().Element(CellStyle).Text("Tarifa Mes").FontSize(9).Bold();

                                IContainer CellStyle(IContainer conta0) => DefaultHeaderStyle(conta0, Colors.Blue.Lighten5);
                            });

                            foreach (var cot in cotizaciones)
                            {
                                var detail = cot.PersonalDetalle ?? new CotizacionPersonalDetalle();

                                table.Cell().Element(CellStyle).AlignLeft().Text(detail.Turno).FontSize(9);
                                table.Cell().Element(CellStyle).Text($"{detail.HorasTurno} hrs").FontSize(9);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {detail.SalarioBase:N2}").FontSize(9);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {detail.ViaticosTotales:N2}").FontSize(9);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {detail.EppTotales:N2}").FontSize(9);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {cot.TarifaAcordada:N2}").FontSize(9).Bold();

                                IContainer CellStyle(IContainer conta1) => DefaultCellStyle(conta1);
                            }
                        });

                        // Detalle por cada turno con desglose y tarifas extras
                        column.Item().PaddingTop(25).Text("Detalles de Tarifas y Cargos Adicionales").FontSize(13).Bold().FontColor(Colors.Blue.Darken3);

                        foreach (var cot in cotizaciones)
                        {
                            var detail = cot.PersonalDetalle ?? new CotizacionPersonalDetalle();

                            column.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(detailCol =>
                            {
                                detailCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text($"Turno: {detail.Turno} ({detail.HorasTurno} Horas)").FontSize(11).Bold().FontColor(Colors.Blue.Darken4);
                                    r.RelativeItem().AlignRight().Text($"Costo Total Mensual: C$ {cot.CostoTotal:N2}").FontSize(10).Bold();
                                });

                                detailCol.Item().PaddingTop(8).Table(tDetail =>
                                {
                                    tDetail.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(4);
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(4);
                                        cols.RelativeColumn(2);
                                    });

                                    // Fila 1
                                    tDetail.Cell().Padding(2).Text("Prestaciones Sociales:").FontSize(9);
                                    tDetail.Cell().Padding(2).AlignRight().Text($"C$ {(detail.SalarioBase * detail.PrestacionesFactor):N2}").FontSize(9);
                                    tDetail.Cell().Padding(2).Text("Supervisión y Estructura:").FontSize(9);
                                    tDetail.Cell().Padding(2).AlignRight().Text($"C$ {detail.Supervision:N2}").FontSize(9);

                                    // Fila 2
                                    tDetail.Cell().Padding(2).Text("Cargos Administrativos:").FontSize(9);
                                    tDetail.Cell().Padding(2).AlignRight().Text($"C$ {detail.Cargos:N2}").FontSize(9);
                                    tDetail.Cell().Padding(2).Text("Seguro Colectivo:").FontSize(9);
                                    tDetail.Cell().Padding(2).AlignRight().Text($"C$ {detail.Seguros:N2}").FontSize(9);

                                    // Fila 3
                                    tDetail.Cell().Padding(2).Text("Gastos Operativos:").FontSize(9);
                                    tDetail.Cell().Padding(2).AlignRight().Text($"C$ {detail.GastosOperativos:N2}").FontSize(9);
                                    tDetail.Cell().Padding(2).Text("Margen Utilidad:").FontSize(9);
                                    tDetail.Cell().Padding(2).AlignRight().Text($"{cot.UtilidadPorcentaje:F1}%").FontSize(9);
                                });

                                // Tarifa sugerida vs acordada
                                detailCol.Item().PaddingTop(10).Row(r =>
                                {
                                    r.RelativeItem().Text($"Tarifa Mensual Sugerida: C$ {cot.TarifaSugerida:N2}").FontSize(9).Italic();
                                    r.RelativeItem().AlignRight().Text($"Tarifa Mensual Acordada: C$ {cot.TarifaAcordada:N2}").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                                });

                                // Items de horas adicionales solicitados por el usuario
                                detailCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                detailCol.Item().PaddingTop(8).Text("Tarifas de Horas Adicionales y Recargos (por hora):").FontSize(9).Bold();

                                detailCol.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Text(t =>
                                    {
                                        t.Span("• Hora Extra Común: ").FontSize(9);
                                        t.Span($"C$ {detail.TarifaExtra:N2}").Bold().FontSize(9);
                                    });
                                    r.RelativeItem().Text(t =>
                                    {
                                        t.Span("• Hora de Día Feriado: ").FontSize(9);
                                        t.Span($"C$ {detail.TarifaFeriado:N2}").Bold().FontSize(9);
                                    });
                                    r.RelativeItem().Text(t =>
                                    {
                                        t.Span("• Hora de Domingo: ").FontSize(9);
                                        t.Span($"C$ {detail.TarifaDomingo:N2}").Bold().FontSize(9);
                                    });
                                });
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        }
    }
}
