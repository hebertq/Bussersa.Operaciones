using BsOperaciones.Application.Common.Interface;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Enum;
using NPOI.SS.UserModel;

namespace BsOperaciones.Application.Common.Strategy.Reloj
{
    public class RelojManualStrategy : RelojStrategyBase, IRelojStrategy
    {
        public TipoReloj Tipo => TipoReloj.Manual;
        public List<HoraEntrada> Parsear(Stream fileStream, DateTime FchaCarga)
        {
            var resultado = new List<HoraEntrada>();
            IWorkbook workbook = WorkbookFactory.Create(fileStream);

            try
            {
                ISheet sheet = workbook.GetSheetAt(0);

                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    var r = sheet.GetRow(i);
                    if (r == null || r.GetCell(0) == null) continue;

                    if (int.TryParse(r.GetCell(0).ToString(), out int id))
                    {
                        // 1. Fecha base y valores económicos
                        string f = r.GetCell(2)?.ToString() ?? FchaCarga.ToShortDateString();
                        double.TryParse(r.GetCell(5)?.ToString(), out double bono);
                        double.TryParse(r.GetCell(6)?.ToString(), out double comida);

                        // 2. Parseo de tiempos
                        DateTime.TryParse($"{f} {r.GetCell(3)}", out DateTime dte);
                        DateTime.TryParse($"{f} {r.GetCell(4)}", out DateTime dts);

                        // --- LÓGICA DE NEGOCIO PARA CRUCE DE DÍA ---
                        // Si la hora de salida es menor que la de entrada (ej: 01:00 < 15:00)
                        // forzamos que la salida pertenezca al día siguiente.
                        if (dts < dte)
                        {
                            dts = dts.AddDays(1);
                        }

                        // 3. Mapeo directo a la clase final
                        resultado.Add(new HoraEntrada
                        {
                            id = id,
                            fecha = dte.ToString("yyyy-MM-dd"),
                            entrada = dte.ToString("HH:mm:ss"),
                            // Ahora dts tiene la fecha correcta, el string saldrá bien
                            salida = dts.ToString("HH:mm:ss"),
                            bono = bono,
                            almuerzocena = comida
                        });
                    }
                }
                return resultado;
            }
            finally
            {
                workbook.Close();
            }
        }
    }
}
