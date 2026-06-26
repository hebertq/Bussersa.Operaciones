using BsOperaciones.Services;
using ColorCode;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BsOperaciones.Component
{
    public partial class CodeSnippet : ComponentBase
    {
        [Inject] ICodeSnippetService CodeSnippetService { get; set; }
        [CascadingParameter] DocsPages _DocsPages { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public bool SetBackground { get; set; }
        [Parameter] public string ClassName { get; set; }
        [Parameter] public RenderFragment Description { get; set; }
        [Parameter] public RenderFragment PagesData { get; set; }

        protected string Code;

        public Guid Id { get; internal set; } = Guid.NewGuid();

        protected override async Task OnInitializedAsync()
        {
            _DocsPages.AddCodeSnippet(this);
            if (!string.IsNullOrWhiteSpace(ClassName) && string.IsNullOrEmpty(Code))
            {
                var formatter = new HtmlClassFormatter();

                var html = await CodeSnippetService.GetCodeSnippet(ClassName);
                var cSharp = "";

                var index = html.IndexOf("@code {");
                if (index > 0)
                {
                    cSharp = html.Substring(index);
                    html = html.Substring(0, index);
                }

                var code = formatter.GetHtmlString(html, Languages.Html);

                if (!string.IsNullOrWhiteSpace(cSharp))
                {
                    code += "<div class='mt-1'>" + formatter.GetHtmlString(cSharp, Languages.CSharp) + "</div>";
                }


                code = HighlightRazor(code);

                Code = code;

            }
        }

        private string HighlightRazor(string code)
        {
            var keywords = new List<string> { "@code", "@inject" };

            var result = code;
            foreach (var keyword in keywords)
            {
                // var rx = new Regex($@"^{keyword}\s");
                // result = rx.Replace(result, @"<span class=""razor"">{keyword}</span>");
                result = result.Replace(keyword, $@"<span class=""razor"">{keyword}</span>");
            }

            return result;
        }
        private string ExampleBackground()
        {
            return SetBackground ? "example-bg" : "";
        }

        public void Dispose()
        {
            _DocsPages.RemoveCodeSnippet(this);
        }
    }
}
