using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Reportes.Componentes
{
    public class HeaderComponent : IComponent
    {
        private readonly string Title = string.Empty;
        private readonly string Name = string.Empty;
        private readonly string Tipo = string.Empty;
        private readonly string DetalleTipo = string.Empty;
        private readonly static Image LogoImage  = Image.FromFile("wwwroot/img/brand/logo.png");
        public HeaderComponent(string titulo,string nombre,string detalle = "",string tipo = "") 
        {
            Title = titulo;
            Name  = nombre;
            Tipo  = tipo;
            DetalleTipo = detalle;
        }
        public void Compose(IContainer container)
        {
            container.Row(row =>
            {
                row.ConstantItem(150).Image(LogoImage);
                row.RelativeItem().Column(column =>
                {
                    column
                        .Item()
                        .PaddingTop(5)
                        .AlignCenter()
                        .Text(Title)   
                        .FontSize(17).SemiBold().FontColor(Colors.Black);
                    if(!string.IsNullOrEmpty(Tipo) && Tipo.Length > 0)
                    {
                        column
                         .Item()
                         .AlignCenter()
                         .Text(text =>
                         {
                             text.Span(DetalleTipo).Bold().FontSize(14);
                             text.Span(Tipo).SemiBold().FontSize(14);
                         });
                    }
                    column
                         .Item()
                         .AlignCenter()
                         .Text(text =>
                                {
                                    text.Span(Name).SemiBold().FontSize(13);
                                });                  
                });
                row.ConstantItem(120);
            });
        }
    }
}
