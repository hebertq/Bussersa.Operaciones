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
            var _Header = new HeaderComponent("PROPUESTA COMERCIAL", clienteNombre, "Fecha de Generación: ", DateTime.Now.ToString("dd/MM/yyyy"));
            var numeroCotizacion = cotizaciones.FirstOrDefault()?.NumeroCotizacion ?? "N/A";

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Margin(35);
                    page.Size(PageSizes.A4);
                    page.Header().Component(_Header);

                    page.Content().Column(column =>
                    {
                        // 1. Datos de Cotización y Cliente (en dos columnas)
                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("CLIENTE / PROPUESTO A:").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                c.Item().Text(clienteNombre).FontSize(11).Bold().FontColor(Colors.Blue.Darken4);
                                c.Item().Text("Servicio de Outsourcing de Personal").FontSize(9).Italic();
                            });
                            row.RelativeItem(1).AlignRight().Column(c =>
                            {
                                c.Item().Text("DOCUMENTO:").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                c.Item().Text("COTIZACIÓN DE SERVICIOS").FontSize(10).Bold().FontColor(Colors.Blue.Darken3);
                                c.Item().Text($"No: {numeroCotizacion}").FontSize(9).Bold().FontColor(Colors.Blue.Darken4);
                                c.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}").FontSize(9);
                                c.Item().Text("Moneda: Córdoba (C$)").FontSize(9);
                            });
                        });

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // 2. Tabla Principal de Items
                        column.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80); // Artículo
                                columns.RelativeColumn(5);  // Descripción
                                columns.ConstantColumn(60); // U. Medida
                                columns.ConstantColumn(60); // Cantidad
                                columns.RelativeColumn(2.5f); // Precio Unitario
                                columns.RelativeColumn(2.5f); // Importe
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Artículo").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).AlignLeft().Text("Descripción").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("U. Medida").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Cantidad").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Precio Unit.").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Importe").FontSize(9).Bold().FontColor(Colors.White);

                                IContainer HeaderStyle(IContainer container) => container
                                    .Border(1)
                                    .BorderColor(Colors.Blue.Darken4)
                                    .Background(Colors.Blue.Darken3)
                                    .Padding(5)
                                    .AlignCenter()
                                    .AlignMiddle();
                            });

                            decimal totalMensual = 0;

                            foreach (var cot in cotizaciones)
                            {
                                var detail = cot.PersonalDetalle ?? new CotizacionPersonalDetalle();
                                int horasMensuales = detail.HorasTurno * 30;
                                decimal precioHoraNormal = horasMensuales > 0 ? cot.TarifaAcordada / horasMensuales : 0;

                                totalMensual += precioHoraNormal + detail.TarifaExtra + detail.TarifaDomingo;

                                // Fila 1: Horas Normales (Servicio Mensual Base)
                                table.Cell().Element(CellStyle).Text("SERV-NORM").FontSize(8);
                                table.Cell().Element(CellStyle).AlignLeft().Text($"Servicio de Personal - Turno {detail.Turno} - Horas Normales").FontSize(8);
                                table.Cell().Element(CellStyle).Text("Hora").FontSize(8);
                                table.Cell().Element(CellStyle).Text("1").FontSize(8);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {precioHoraNormal:N2}").FontSize(8);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {precioHoraNormal:N2}").FontSize(8).Bold();

                                // Fila 2: Hora Extra
                                table.Cell().Element(CellStyle).Text("REC-EXTRA").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).AlignLeft().Text($"Recargo por Hora Extra Común (Turno {detail.Turno})").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).Text("Hora").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).Text("1").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {detail.TarifaExtra:N2}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {detail.TarifaExtra:N2}").FontSize(8).FontColor(Colors.Grey.Darken1).Bold();

                                // Fila 3: Feriados y Domingos (Unificados en una sola fila)
                                table.Cell().Element(CellStyle).Text("REC-FD").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).AlignLeft().Text($"Recargo por Hora de Domingo y Día Feriado (Turno {detail.Turno})").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).Text("Hora").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).Text("1").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {detail.TarifaDomingo:N2}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                table.Cell().Element(CellStyle).AlignRight().Text($"C$ {detail.TarifaDomingo:N2}").FontSize(8).FontColor(Colors.Grey.Darken1).Bold();

                                IContainer CellStyle(IContainer container) => container
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten3)
                                    .Padding(5)
                                    .AlignCenter()
                                    .AlignMiddle();
                            }

                            decimal subtotal = totalMensual;
                            decimal iva = subtotal * 0.15m;
                            decimal totalConIva = subtotal + iva;

                            // Fila de Sub-total
                            table.Cell().ColumnSpan(5).BorderTop(1).BorderColor(Colors.Grey.Darken2).Padding(4).AlignRight().Text("SUB-TOTAL:").FontSize(9).Bold();
                            table.Cell().BorderTop(1).BorderColor(Colors.Grey.Darken2).Padding(4).AlignRight().Text($"C$ {subtotal:N2}").FontSize(9).Bold();

                            // Fila de IVA
                            table.Cell().ColumnSpan(5).Padding(4).AlignRight().Text("I.V.A. (15%):").FontSize(9).Bold();
                            table.Cell().Padding(4).AlignRight().Text($"C$ {iva:N2}").FontSize(9).Bold();

                            // Fila de Total con IVA
                            table.Cell().ColumnSpan(5).Padding(6).AlignRight().Text("TOTAL:").FontSize(10).Bold().FontColor(Colors.Blue.Darken4);
                            table.Cell().Padding(6).AlignRight().Text($"C$ {totalConIva:N2}").FontSize(10).Bold().FontColor(Colors.Blue.Darken3);
                        });

                        // 3. Notas y Condiciones de la Cotización
                        column.Item().PaddingTop(25).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(notes =>
                        {
                            notes.Item().Text("TÉRMINOS Y CONDICIONES COMERCIALES").FontSize(9).Bold().FontColor(Colors.Blue.Darken4);
                            notes.Item().PaddingTop(4).Text("• Las tarifas de recargos por horas adicionales son de referencia y solo se facturarán según las horas efectivamente laboradas y reportadas por el cliente.").FontSize(8);
                            notes.Item().PaddingTop(2).Text("• Validez de esta propuesta comercial: 15 días a partir de la fecha de generación.").FontSize(8);
                            notes.Item().PaddingTop(2).Text("• Forma de pago: Crédito a 15 días posteriores a la fecha de facturación.").FontSize(8);
                        });
                    });

                    page.Footer().AlignCenter().Column(f =>
                    {
                        f.Item().AlignCenter().Text("BUSSERSA - Business Solution Services • info@bussersa.com • www.bussersa.com").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        }
    }
}
