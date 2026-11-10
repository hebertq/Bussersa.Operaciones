using System;
using System.Collections.Generic;
using System.Linq;
using Modelo.Entidades.Entradas.Odoo;

namespace BsOperaciones.Application.Common.Strategy.Reloj
{
    public abstract class RelojStrategyBase
    {
        protected List<HoraEntrada> AgruparMarcaciones(List<(int id, DateTime dt)> marcas, DateTime fechaReferencia)
        {
            var resultado = new List<HoraEntrada>();

            // Deduplicar marcas idénticas en la misma fecha y hora
            marcas = marcas.Distinct().ToList();

            // 1. Agrupamos por ID de empleado
            var grupos = marcas.GroupBy(x => x.id);

            foreach (var grupo in grupos)
            {
                // 2. Ordenamos TODAS las marcas del empleado cronológicamente
                var ordenadas = grupo.OrderBy(x => x.dt).ToList();

                // Marcas que inician en el día de la carga
                var marcasDelDia = ordenadas.Where(x => x.dt.Date == fechaReferencia.Date).ToList();
                if (!marcasDelDia.Any()) continue;

                // Primer registro: Entrada del día
                var primerRegistro = marcasDelDia.First().dt;

                // Marcas posteriores dentro de un turno lógico (< 20 horas, contemplando posibles turnos nocturnos)
                var marcasPosteriores = ordenadas
                    .Where(x => x.dt > primerRegistro && (x.dt - primerRegistro).TotalHours < 20)
                    .ToList();

                DateTime? salidaEncontrada = null;

                if (marcasPosteriores.Any())
                {
                    // Tomamos el último registro si supera 1 hora respecto a la entrada
                    var ultimoRegistro = marcasPosteriores.Last().dt;
                    double diferenciaHoras = (ultimoRegistro - primerRegistro).TotalHours;

                    if (diferenciaHoras >= 1)
                    {
                        salidaEncontrada = ultimoRegistro;
                    }
                }

                // Generamos un solo registro por empleado para la fecha
                resultado.Add(new HoraEntrada
                {
                    id = grupo.Key,
                    fecha = primerRegistro.ToString("yyyy-MM-dd"),
                    entrada = primerRegistro.ToString("HH:mm:ss"),
                    salida = salidaEncontrada?.ToString("HH:mm:ss") ?? "00:00:00"
                });
            }

            return resultado;
        }        
    }
}
