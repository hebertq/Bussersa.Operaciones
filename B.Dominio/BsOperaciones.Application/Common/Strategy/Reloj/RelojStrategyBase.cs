using Modelo.Entidades.Entradas.Odoo;

namespace BsOperaciones.Application.Common.Strategy.Reloj
{
    public abstract class RelojStrategyBase
    {
        protected List<HoraEntrada> AgruparMarcaciones(List<(int id, DateTime dt)> marcas, DateTime fechaReferencia)
        {
            var resultado = new List<HoraEntrada>();

            // 1. Agrupamos por ID de empleado
            var grupos = marcas.GroupBy(x => x.id);

            foreach (var grupo in grupos)
            {
                // 2. Ordenamos TODAS las marcas del empleado cronológicamente
                var ordenadas = grupo.OrderBy(x => x.dt).ToList();

                for (int i = 0; i < ordenadas.Count; i++)
                {
                    var marcaActual = ordenadas[i].dt;

                    // REGLA DE ORO: Solo procesamos marcas que INICIAN el día de la carga.
                    // Si la marca es de otro día (ej. el 29), la ignoramos como inicio de turno.
                    if (marcaActual.Date != fechaReferencia.Date) continue;

                    DateTime? salidaEncontrada = null;

                    // 3. Buscamos la siguiente marca para ver si es una salida válida (entre 3 y 14 horas después)
                    if (i + 1 < ordenadas.Count)
                    {
                        var siguienteMarca = ordenadas[i + 1].dt;
                        double diferenciaHoras = (siguienteMarca - marcaActual).TotalHours;

                        // Si la siguiente marca está en un rango lógico de turno laboral
                        if (diferenciaHoras >= 1 && diferenciaHoras < 20)
                        {
                            salidaEncontrada = siguienteMarca;
                            // Avanzamos el puntero para no usar la salida como una nueva entrada
                            i++;
                        }
                    }

                    // 4. Agregamos el registro resultante
                    resultado.Add(new HoraEntrada
                    {
                        id = grupo.Key,
                        fecha = marcaActual.ToString("yyyy-MM-dd"), // Siempre queda la fecha de inicio
                        entrada = marcaActual.ToString("HH:mm:ss"),
                        salida = salidaEncontrada?.ToString("HH:mm:ss") ?? "00:00:00"
                    });
                }
            }
            return resultado;
        }        
    }
}
