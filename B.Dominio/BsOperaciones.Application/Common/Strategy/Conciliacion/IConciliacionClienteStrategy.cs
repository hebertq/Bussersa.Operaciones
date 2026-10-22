using System;
using System.Collections.Generic;
using System.IO;
using Modelo.Report;

namespace BsOperaciones.Application.Common.Strategy.Conciliacion
{
    public interface IConciliacionClienteStrategy
    {
        int OperacionId { get; }
        string NombreCliente { get; }
        string NombreFormatoArchivo { get; }
        bool SoportaOperacion(int operacionId, string? nombreOperacion);
        ResultadoConciliacionDto Conciliar(Stream fileStream, List<ReporteCierreMarcadasDetalle> marcadasBussersa, DateTime? fchaInicio, DateTime? fchaFin);
    }
}
