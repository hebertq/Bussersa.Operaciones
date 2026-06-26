
using Modelo.Enum;
using System;

namespace Modelo.Entidades.Entradas.Odoo
{
    public class HoraEntrada
    {
        public HoraEntrada()
        {
            id = 0;
            idmarca = 0;
            nombre = string.Empty;
            // Inicializar con una fecha válida por defecto para evitar el "00"
            fecha = DateTime.Now.ToString("yyyy-MM-dd");
        }

        public int id { set; get; }
        public int idmarca { get; set; } = 0;
        public string entrada { set; get; }
        public string salida { set; get; }

        // Cambiamos la lógica de fecha para que no permita formatos inválidos
        private string _fecha;
        public string fecha
        {
            get => _fecha;
            set
            {
                // Si el valor viene como "2026-00-02", lo corregimos a un mes válido (01)
                if (!string.IsNullOrEmpty(value) && value.Contains("-00-"))
                {
                    _fecha = value.Replace("-00-", "-01-");
                }
                else
                {
                    _fecha = value;
                }
            }
        }

        public string nombre { set; get; }
        public double bono { set; get; } = 0;
        public double almuerzocena { set; get; } = 0;
        public EstadoMarcacion Estado => DeterminarEstado();

        private EstadoMarcacion DeterminarEstado()
        {
            bool tieneEntrada = !string.IsNullOrEmpty(entrada) && entrada != "00:00:00";
            bool tieneSalida = !string.IsNullOrEmpty(salida) && salida != "00:00:00";

            if (tieneEntrada && tieneSalida) return EstadoMarcacion.Completo;
            if (!tieneEntrada) return EstadoMarcacion.FaltaEntrada;
            return EstadoMarcacion.FaltaSalida;
        }

        // Usamos la fecha ya validada
        public string entradastr => $"{_fecha} {entrada}";
        public string salidastr => $"{_fecha} {salida}";
    }

    public class EntradaSalidacsv
    {
        public EntradaSalidacsv()
        {
            id = 0;
        }
        public int id { set; get; }
        public DateTime entradasalida { set; get; }
        public string fecha { set; get; }
        public bool entrada { set; get; } = false;
        public bool salida { set; get; } = false;
    }
}
