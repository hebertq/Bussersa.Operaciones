using BsOperaciones.Application.Common.Interface;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Enum;
using BsOperaciones.Application.Common.Strategy.Reloj;
using NPOI.SS.UserModel;

namespace BsOperaciones.Application.Common.Strategy
{
    public class RelojWMStrategy : RelojStrategyBase, IRelojStrategy
    {
        public TipoReloj Tipo => TipoReloj.WaltMart;

        public List<HoraEntrada> Parsear(Stream fileStream, DateTime FchaCarga)
        {
            var marcasCrudas = new List<(int id, DateTime dt)>();

            // Usamos la misma estructura robusta: crear el workbook y asegurar su cierre en el finally
            IWorkbook workbook = WorkbookFactory.Create(fileStream);
            try
            {
                // REGLA: Walmart usa la Hoja índice 2
                ISheet sheet = workbook.GetSheetAt(2);

                // REGLA: Validación de la fecha en la fila 2, celda 2
                string? diaStr = sheet.GetRow(2)?.GetCell(2)?.ToString();
                if (string.IsNullOrEmpty(diaStr)) return [];

                // Simplificación usando Range para obtener los primeros 10 caracteres (yyyy-MM-dd)
                string diaLimpio = diaStr.Length > 10 ? diaStr[..10] : diaStr;
                DateTime fechaArchivo = DateTime.Parse(diaLimpio);

                // Validamos que el archivo corresponda a la fecha que el usuario seleccionó en la UI
                if (fechaArchivo.Date != FchaCarga.Date) return [];

                int? currentId = null;

                // REGLA: El procesamiento inicia en la fila 4
                for (int i = 4; i <= sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null) continue;

                    if (i % 2 == 0) // FILA PAR: Contiene el ID del empleado (Celda 2)
                    {
                        var cellId = row.GetCell(2)?.ToString();
                        if (int.TryParse(cellId, out int id))
                            currentId = id;
                    }
                    else // FILA IMPAR: Contiene las horas (Celda 0)
                    {
                        if (currentId == null) continue;

                        string? hStr = row.GetCell(0)?.ToString();
                        if (!string.IsNullOrEmpty(hStr) && hStr.Length >= 5)
                        {
                            // Extraemos la primera marcación (Entrada)
                            var hEntrada = hStr[..5];
                            marcasCrudas.Add((currentId.Value, DateTime.Parse($"{diaLimpio} {hEntrada}:00")));

                            // Si el string es largo (ej: "08:0017:00"), extraemos la segunda (Salida)
                            if (hStr.Length >= 10)
                            {
                                var hSalida = hStr.Substring(hStr.Length - 5);
                                marcasCrudas.Add((currentId.Value, DateTime.Parse($"{diaLimpio} {hSalida}:00")));
                            }
                        }
                        currentId = null; // Limpiamos el ID para el siguiente par de filas
                    }
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
    }
}