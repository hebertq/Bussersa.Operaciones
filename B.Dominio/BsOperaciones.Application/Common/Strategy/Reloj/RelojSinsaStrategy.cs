using BsOperaciones.Application.Common.Interface;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Enum;
using NPOI.SS.UserModel;

namespace BsOperaciones.Application.Common.Strategy.Reloj
{
    public class RelojSinsaStrategy : RelojStrategyBase, IRelojStrategy
    {
        public TipoReloj Tipo => TipoReloj.Sinsa;

        public List<HoraEntrada> Parsear(Stream fileStream, DateTime FchaCarga)
        {
            var marcasCrudas = new List<(int id, DateTime dt)>();
            // Usamos la misma estructura robusta: crear el workbook y asegurar su cierre en el finally
            IWorkbook workbook = WorkbookFactory.Create(fileStream);
            try
            {
                // REGLA: Walmart usa la Hoja índice 2
                ISheet sheet = workbook.GetSheetAt(1);
                int? currentId = null;

                var columnaDia = ObtenerIndiceColumnaPorDia(sheet, FchaCarga.Day);
                if (columnaDia == null) return [];
                // REGLA: El procesamiento inicia en la fila 7
                for (int i = 7; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null) continue;

                    var cellId = row.GetCell(0)?.ToString();
                    if (int.TryParse(cellId, out int id))
                        currentId = id;

                    if (currentId == null) continue;

                    string? hStr = row.GetCell((int)columnaDia)?.ToString();
                    if (!string.IsNullOrEmpty(hStr))
                    {
                        // Extraemos la primera marcación (Entrada)
                        var marcaciones = hStr.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var marca in marcaciones)
                        {
                            if (TimeSpan.TryParse(marca.Trim(), out TimeSpan hora))
                            {
                                // Combinamos la fecha de carga con la hora encontrada
                                marcasCrudas.Add((id, FchaCarga.Date.Add(hora)));
                            }
                        }               
                    }
                    currentId = null; // Limpiamos el ID para el siguiente par de filas
                }

                // Enviamos las marcas a la clase base para que resuelva los turnos y agrupaciones
                return AgruparMarcaciones(marcasCrudas, FchaCarga);
            }
            finally
            {
                // Garantizamos la liberación del recurso de Excel
                workbook.Close();
            }
        }

        public int? ObtenerIndiceColumnaPorDia(ISheet sheet, int diaBuscado)
        {
            // En los archivos de "Attendance Record", los días están en la fila 4
            IRow filaEncabezado = sheet.GetRow(4);
            if (filaEncabezado == null) return null;

            // Recorremos desde la columna 3 (donde terminan Dept/Name) hasta el final
            for (int i = 3; i < filaEncabezado.LastCellNum; i++)
            {
                var celda = filaEncabezado.GetCell(i);
                if (celda == null) continue;

                // Limpiamos el valor por si tiene espacios o texto
                string valor = celda.ToString()!.Trim();

                // Si el valor de la celda es igual al día que buscas (ej: "26")
                if (int.TryParse(valor, out int diaEnExcel))
                {
                    if (diaEnExcel == diaBuscado)
                    {
                        return i; // Este es el número de columna (celda) que usarás
                    }
                }
            }
            return null; // No se encontró el día en esta hoja
        }
    }
}
