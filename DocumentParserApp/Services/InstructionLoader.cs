using System.IO;
using System.Threading.Tasks;

namespace DocumentParserApi.Services
{
    public class InstructionLoader
    {
        public static async Task<string> GetInstructionsAsync(string type)
        {
            var path = Path.Combine("Instructions", $"{type}.json");
            return await File.ReadAllTextAsync(path);
        }
        public static async Task<string> GetKnowledgeBase(string type)
        {
            var path = Path.Combine("Helpers", $"{type}_knowledge.txt");
            return await File.ReadAllTextAsync(path);
        }
        public static async Task<string> GetProcessInstructions(string type)
        {
            var path = Path.Combine("Helpers", $"{type}_instruction.txt");
            return await File.ReadAllTextAsync(path);
        }
    }
}
