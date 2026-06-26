using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BsOperaciones.Component
{
    public partial class DocsPages : ComponentBase
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public RenderFragment Description { get; set; }
        [Parameter] public Type ComponentType { get; set; }
        [Parameter] public string IdPage { get; set; }
        [Parameter] public bool ShowSnippet { get; set; }

        private async Task NavigateTo(CodeSnippet codeSnippet)
        {
            if (codeSnippet?.Id != null)
            {
                await JSRuntime.InvokeVoidAsync("scrollToFragment", codeSnippet.Id.ToString());
            }
        }

        public List<CodeSnippet> CodeSnippets = new List<CodeSnippet>();

        public void AddCodeSnippet(CodeSnippet codeSnippet)
        {
            if (codeSnippet != null)
                CodeSnippets.Add(codeSnippet);

            StateHasChanged();
        }

        public void RemoveCodeSnippet(CodeSnippet codeSnippet)
        {
            CodeSnippets.Remove(codeSnippet);
            StateHasChanged();
        }
    }
}

