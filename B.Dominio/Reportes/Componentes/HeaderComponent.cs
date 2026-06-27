using System;
using System.IO;
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
        private readonly static Image LogoImage = LoadLogo();

        private static Image LoadLogo()
        {
            string[] paths = new[]
            {
                "wwwroot/img/brand/logo.png",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot/img/brand/logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png"),
                "logo.png"
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        return Image.FromFile(path);
                    }
                    catch { }
                }
            }

            // Fallback to a tiny 1x1 transparent PNG byte array to prevent throwing exceptions
            byte[] transparentPng = new byte[] {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
                0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
                0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
                0x42, 0x60, 0x82
            };
            return Image.FromBinaryData(transparentPng);
        }

        public HeaderComponent(string titulo, string nombre, string detalle = "", string tipo = "") 
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
                // Compact logo on the left (width 95)
                row.ConstantItem(95).Image(LogoImage);

                // Centered text content
                row.RelativeItem().Column(column =>
                {
                    column
                        .Item()
                        .AlignCenter()
                        .Text(Title)
                        .FontSize(15).SemiBold().FontColor(Colors.Black);

                    if (!string.IsNullOrEmpty(Tipo) && Tipo.Length > 0)
                    {
                        column
                          .Item()
                          .AlignCenter()
                          .Text(text =>
                          {
                              text.Span(DetalleTipo).Bold().FontSize(12);
                              text.Span(Tipo).SemiBold().FontSize(12);
                          });
                    }

                    column
                          .Item()
                          .AlignCenter()
                          .Text(text =>
                          {
                              text.Span(Name).SemiBold().FontSize(11);
                          });
                });

                // Balancing space on the right (width 95)
                row.ConstantItem(95);
            });
        }
    }
}
