using BsOperaciones.Application.Common.Interface;
using Microsoft.Extensions.DependencyInjection;
using Modelo.Enum;

namespace BsOperaciones.Application.Common.Strategy.Reloj
{
    public class RelojSelector
    {
        private readonly IServiceProvider _serviceProvider;

        public RelojSelector(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IRelojStrategy ObtenerEstrategia(TipoReloj tipo)
        {
            return tipo switch
            {
                TipoReloj.WaltMart => ActivatorUtilities.CreateInstance<RelojWMStrategy>(_serviceProvider),
                TipoReloj.LasConde => ActivatorUtilities.CreateInstance<RelojLasCondeStrategy>(_serviceProvider),
                TipoReloj.Manual => ActivatorUtilities.CreateInstance<RelojManualStrategy>(_serviceProvider),
                TipoReloj.Sinsa => ActivatorUtilities.CreateInstance<RelojSinsaStrategy>(_serviceProvider),              
                // Cuando agregues el 4to reloj, solo añades una línea aquí:
                // TipoReloj.NuevoBiometrico => ActivatorUtilities.CreateInstance<NuevoRelojStrategy>(_serviceProvider),
                _ => throw new ArgumentException($"El tipo de reloj {tipo} no está registrado en el selector.")
            };
        }
    }
}
