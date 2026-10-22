using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace BsOperaciones.Application.Common.Strategy.Conciliacion
{
    public class ConciliacionClienteSelector
    {
        private readonly IServiceProvider _serviceProvider;

        public ConciliacionClienteSelector(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool SoportaConciliacion(int operacionId, string? nombreOperacion = null)
        {
            return ObtenerEstrategia(operacionId, nombreOperacion) != null;
        }

        public IConciliacionClienteStrategy? ObtenerEstrategia(int operacionId, string? nombreOperacion = null)
        {
            // Estrategia Sinsa (ID 23)
            var sinsaStrategy = _serviceProvider.GetService<ConciliacionSinsaStrategy>() 
                ?? ActivatorUtilities.CreateInstance<ConciliacionSinsaStrategy>(_serviceProvider);

            if (sinsaStrategy.SoportaOperacion(operacionId, nombreOperacion))
            {
                return sinsaStrategy;
            }

            // Aquí se pueden agregar futuras estrategias: Walmart, Las Condes, CBC, etc.

            return null;
        }
    }
}
