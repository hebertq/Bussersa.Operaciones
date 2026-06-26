using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Modelo.ClasesGenericas
{
    public class TiempoJob
    {
        [XmlAttribute(AttributeName = "Nombre")]
        public string Proceso { set; get; }
        public DateTime Inicio { set; get; } = DateTime.Now;
        public DateTime? Fin { set; get; }
        public double TiempoEspera { set; get; } = 0;
        public int Registros { set; get; }
        public List<SubProceso> SubProc { set; get; } = new List<SubProceso>();
        public void FinProc()
        {
            Fin = DateTime.Now;
            TiempoEspera = Math.Round((Fin - Inicio).Value.TotalMinutes, 2);
        }
    }

    public class LogJob
    {
        [XmlAttribute(AttributeName = "Servicio")]
        string NombreServicio { set; get; } = "BIZEMINI";
        public DateTime Inicio { set; get; } = DateTime.Now;
        public DateTime? Fin { set; get; }
        public double TiempoProceso { set; get; } = 0;
        public List<TiempoJob> Proceso { set; get; } = new List<TiempoJob>();
        public void FinProc()
        {
            Fin = DateTime.Now;
            TiempoProceso = Math.Round((Fin - Inicio).Value.TotalMinutes, 2);
        }

        public void InicioProceso(string pnombre, int pregistro)
        {
            var proc = new TiempoJob { Proceso = pnombre, Registros = pregistro };
            Proceso.Add(proc);
        }
        public void FinProcesoHijo(string pnombre)
        {
            foreach (var tr in Proceso.Where(x => x.Proceso == pnombre))
            {
                tr.FinProc();
            }
        }

        public void subProceso(string padre, string pnombre, int pregistro, JobType ptipo, string perror)
        {
            foreach (var tr in Proceso.Where(x => x.Proceso == padre))
            {
                var proc = new SubProceso { Nombre = pnombre, Registros = pregistro, ErrorProc = perror, Tipo = ptipo.ToString() };
                tr.SubProc.Add(proc);
            }
        }
    }

    public class SubProceso
    {
        [XmlAttribute(AttributeName = "Nombre")]
        public string Nombre { set; get; } = string.Empty;
        public DateTime Inicio { set; get; } = DateTime.Now;
        public string Tipo { set; get; }
        public int Registros { set; get; } = 0;
        public string ErrorProc { set; get; } = string.Empty;
    }

    public enum JobType
    {
        Inserta = 1,
        Actualiza = 2,
        Servicio = 3
    }
}
