using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Componentes;

namespace Reportes.Operaciones
{
    public class NominaxPagarDocument 
    {
        public NominaxPagarDocument(){}

        public ISingleResponse<FileNameString> PrintPdf(string nombre, repnominapago modelo)
        {
            ISingleResponse<FileNameString> response = new SingleResponse<FileNameString>();
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var efectivo = modelo.nominatotal.Where(x => x.tarjeta == false).ToList();
                var tarjeta = modelo.nominatotal.Where(x => x.tarjeta == true).ToList();
                var desgloce = efectivo.Count > 0 ? modelo.desgloceefectivo : new List<nominadesgefectivo>();

                var pdfdocument = Document.Merge(GenerateReport(nombre, "efectivo", efectivo.Count > 0?efectivo:new List<nominaall>()),
                                                  GenerateReport(nombre, "tarjeta", tarjeta.Count > 0 ? tarjeta : new List<nominaall>()),
                                                  GenerateReporteDesgloce(nombre, desgloce)
                                                  ).GeneratePdf();

                response.Model.File = Convert.ToBase64String(pdfdocument.ToArray());
            }
            catch(Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Servicio, ex, "NominaxPagarDocument");
            }

            return response;
        }
        private static Document GenerateReport(string nombre, string tipo, List<nominaall> modelo)
        {
            var _Header = new HeaderComponent("DETALLE DE NOMINA", nombre, "Tipo de nomina: ", tipo);
            IContainer DefaultCellStyle(IContainer container)
            {
                return container
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Medium)
                    .Padding(5)
                    .AlignCenter()
                    .AlignMiddle();
            }

            IContainer DefaultHeaderStyle(IContainer container, string backgroundColor)
            {
                return container
                    .Border(1)
                    .BorderColor(Colors.Grey.Darken1)
                    .Background(backgroundColor)
                    .Padding(5)
                    .AlignCenter()
                    .AlignMiddle();
            }

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Margin(15);
                    page.Size(PageSizes.A4.Landscape());
                    page.Header().AlignCenter()
                                 .AlignMiddle()
                                 .Component(_Header);
                    page.Content().Element(container  => 
                    {
                        container.PaddingVertical(15).Column(column =>
                        {
                            column.Item().Element(conta => {
                                conta
                                .Padding(10)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(90);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(35);
                                        columns.ConstantColumn(40);
                                        columns.ConstantColumn(35);
                                        columns.ConstantColumn(35);
                                        columns.ConstantColumn(35);
                                        columns.ConstantColumn(30);
                                        columns.ConstantColumn(40);
                                        columns.ConstantColumn(40);
                                        columns.ConstantColumn(40);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(35);
                                        columns.ConstantColumn(35);
                                        columns.ConstantColumn(40);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(50);
                                        columns.ConstantColumn(65);
                                    });
                                
                                    table.Header(header =>
                                    {
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Nombre").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Salario Base").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Dias Lab").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Salario Basico").FontSize(8);
                                
                                        header.Cell().ColumnSpan(3).Element(CellStyle).AlignCenter().Text("Bono").FontSize(8);
                                        header.Cell().ColumnSpan(2).Element(CellStyle).AlignCenter().Text("Horas extras").FontSize(8);
                                        header.Cell().ColumnSpan(2).Element(CellStyle).AlignCenter().Text("Ingresos").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Total Ingresos").FontSize(8);
                                        header.Cell().ColumnSpan(3).Element(CellStyle).AlignCenter().Text("Deducciones").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Total Deduc").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Salario Neto").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Recibi conform").FontSize(8);
                                
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Trans").FontSize(8);
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Alim").FontSize(8);
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Bcn").FontSize(8);
                                
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Cant").FontSize(8);
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Pago").FontSize(8);
                                
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Vac").FontSize(8);
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Otros").FontSize(8);
                                
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Inss").FontSize(8);
                                        header.Cell().Element(CellStyle).AlignCenter().Text("IR").FontSize(8);
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Otras").FontSize(8);
                                
                                        // you can extend existing styles by creating additional methods
                                        IContainer CellStyle(IContainer conta0) => DefaultHeaderStyle(conta0, Colors.Grey.Lighten3);
                                    });
                                
                                    foreach (var item in modelo)
                                    {
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignLeft().Text($"{item.empleado}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.basico}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.dias}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.salbasico}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.transporte}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.alimento}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.bonocump}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.he}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.horaextra}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.vacaciones}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.otrosing}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.totaldev}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.inss}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.ir}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.otdeduc}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.totalded}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.neto}").FontSize(7);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text("").FontSize(7);
                                
                                        IContainer CellStyle(IContainer conta1) => DefaultCellStyle(conta1).ShowOnce();
                                    }
                                });
                            });
                        });
                    });
                });
            });
        }

        private static Document GenerateReporteDesgloce(string nombre, List<nominadesgefectivo> modelo)
        {
            var _Header = new HeaderComponent("DETALLE DE NOMINA", nombre, "Desgloce de: ", "efectivo");
            IContainer DefaultCellStyle(IContainer container)
            {
                return container
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Medium)
                    .Padding(5)
                    .AlignCenter()
                    .AlignMiddle();
            }

            IContainer DefaultHeaderStyle(IContainer container, string backgroundColor)
            {
                return container
                    .Border(1)
                    .BorderColor(Colors.Grey.Darken1)
                    .Background(backgroundColor)
                    .Padding(5)
                    .AlignCenter()
                    .AlignMiddle();
            }

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Margin(15);
                    page.Size(PageSizes.A4.Landscape());
                    page.Header().AlignCenter()
                                 .AlignMiddle()
                                 .Component(_Header);
                    page.Content().Element(container =>
                    {
                        container.PaddingVertical(15).Column(column =>
                        {
                            column.Item().Element(conta => {
                                conta
                                .Padding(10)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(180);
                                        columns.ConstantColumn(55);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(45);
                                        columns.ConstantColumn(55);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Cedula").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Nombre").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Salario Neto").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Billetes C$ 1000").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Billetes C$ 500").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Billetes C$ 200").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Billetes C$ 100").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Billetes C$ 50").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Billetes C$ 20").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Billetes C$ 10").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Moneda C$ 5").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Moneda C$ 1").FontSize(8);
                                        header.Cell().RowSpan(2).Element(CellStyle).ExtendHorizontal().AlignCenter().Text("Neto a recibir").FontSize(8);
                                        // you can extend existing styles by creating additional methods
                                        IContainer CellStyle(IContainer conta0) => DefaultHeaderStyle(conta0, Colors.Grey.Lighten3);
                                    });

                                    foreach (var item in modelo)
                                    {
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignLeft().Text($"{item.cedula}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignLeft().Text($"{item.empleado}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{item.neto}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bi1000}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bi500}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bi200}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bi100}").FontSize(8);                                        
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bi50}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bi20}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bi10}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bi5}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bi1}").FontSize(8);
                                        table.Cell().Element(CellStyle).ExtendHorizontal().AlignRight().Text($"{(int)item.bitotal}").FontSize(8);

                                        IContainer CellStyle(IContainer conta1) => DefaultCellStyle(conta1).ShowOnce();
                                    }                                                                    
                                });                               
                            });
                            if(modelo.Count > 0)
                            {
                                column.Item().Element(conta => {
                                    conta.PaddingLeft(20).Row(row =>
                                    {
                                        var Totales = (from ef in modelo
                                                       group ef by 1
                                                          into e
                                                       select new
                                                       {
                                                           bi1 = (int)e.Sum(x => x.bi1),
                                                           bi5 = (int)e.Sum(x => x.bi5),
                                                           bi10 = (int)e.Sum(x => x.bi10),
                                                           bi20 = (int)e.Sum(x => x.bi20),
                                                           bi50 = (int)e.Sum(x => x.bi50),
                                                           bi100 = (int)e.Sum(x => x.bi100),
                                                           bi200 = (int)e.Sum(x => x.bi200),
                                                           bi500 = (int)e.Sum(x => x.bi500),
                                                           bi1000 = (int)e.Sum(x => x.bi1000),
                                                           bitotal = (int)e.Sum(x => x.bitotal),
                                                           neto = e.Sum(x => x.neto)
                                                       }).ToList().FirstOrDefault();

                                        row.ConstantItem(80).PaddingLeft(15).Text("Totales").FontSize(10).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(180).PaddingLeft(15).Text("").FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(55).PaddingLeft(15).Text("").FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(45).PaddingLeft(15).Text(Totales!.bi1000.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(45).PaddingLeft(15).Text(Totales.bi500.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(45).PaddingLeft(15).Text(Totales.bi200.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(45).PaddingLeft(15).Text(Totales.bi100.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(45).PaddingLeft(15).Text(Totales.bi50.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(45).PaddingLeft(15).Text(Totales.bi20.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(45).PaddingLeft(15).Text(Totales.bi10.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(45).PaddingLeft(15).Text(Totales.bi5.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(45).PaddingLeft(15).Text(Totales.bi1.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                        row.ConstantItem(55).PaddingLeft(5).Text(Totales.bitotal.ToString()).FontSize(8).SemiBold().FontColor(Colors.Black);
                                    });
                                });
                            }                            
                        });
                    });
                });
            });
        }       
    }
}
