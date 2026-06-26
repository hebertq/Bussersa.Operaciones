using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BsOperaciones.Component.TypeBrowsers
{
    public partial class TypeBrowser : ComponentBase
    {
        [Inject] private IDialogReference modalService { get; set; }
        [Parameter] public Type Type { get; set; }

        private IList<PropertyView> properties;
        private List<MethodInfo> methods;

        protected override void OnInitialized()
        {
            if (Type == null)
            {
                return;
            }

            //modalService.u(Type.GetFriendlyName());

            properties = Type.GetProperties().Select(e => new PropertyView(e)).ToList();

            methods = Type
                .GetMethods()
                .Where(m => !m.IsSpecialName && !m.IsVirtual && m.MethodImplementationFlags != MethodImplAttributes.InternalCall)
                .ToList();
        }

        private void Cerrar() => modalService?.Close();
    }
}
