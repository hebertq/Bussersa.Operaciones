using BsOperaciones.Application.Common.Interface;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Enum;
using System.Text;

namespace BsOperaciones.Application.Common.Strategy.Reloj
{
    public class RelojLasCondeStrategy :RelojStrategyBase, IRelojStrategy
    {
        public TipoReloj Tipo => TipoReloj.LasConde;
        public List<HoraEntrada> Parsear(Stream fileStream, DateTime FchaCarga)
        {
            var marcasCrudas = new List<(int id, DateTime dt)>();

            // IMPORTANTE: No usar NPOI (WorkbookFactory) porque el archivo es CSV
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                // Leer la primera línea (encabezados) para saltarla
                string? header = reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Separar por comas (formato CSV)
                    var columnas = line.Split(',');

                    // Según tu archivo: sJobNo esta en index 1, Date en 3 y Time en 4
                    if (columnas.Length >= 5)
                    {
                        string sJobNo = columnas[1].Trim();
                        string sDate = columnas[3].Trim();
                        string sTime = columnas[4].Trim();

                        if (int.TryParse(sJobNo, out int idEmpleado))
                        {
                            // Combinamos fecha y hora: "2025-04-04 13:55:05"
                            if (DateTime.TryParse($"{sDate} {sTime}", out DateTime fechaHora))
                            {
                                marcasCrudas.Add((idEmpleado, fechaHora));
                            }
                        }
                    }
                }
            }

            // Llamamos a la lógica de la clase base para agrupar entradas y salidas
            // pasándole la lista cruda y la fecha que seleccionó el usuario
            return AgruparMarcaciones(marcasCrudas, FchaCarga);
        }
    }
}

