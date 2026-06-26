using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Reportes.Componentes
{
    public static class ExtensionLabel
    {
        private static IContainer Cell(this IContainer container, bool dark)
        {
            return container
                .Border(1)
                //.Background(dark ? Colors.Grey.Lighten2 : Colors.White)
                .AlignMiddle()
                .AlignCenter()
                .Padding(3);
        }

        // displays only text label
        public static void LabelCell(this IContainer container, string text) => Cell(container,true).Text(text).FontSize(10);

        // allows you to inject any type of content, e.g. image
        public static IContainer ValueCell(this IContainer container) => Cell(container, false);
    }
}
