using System.Threading.Tasks;

namespace BsOperaciones.Services
{
    public interface ICodeSnippetService
    {
        public Task<string> GetCodeSnippet(string className);
    }

    public class FakeSnippetService : ICodeSnippetService
    {
        public Task<string> GetCodeSnippet(string className)
        {
            return Task.FromResult("Source code view is disabled");
        }
    }
}
