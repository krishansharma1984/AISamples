using Azure.AI.OpenAI;
using DocumentParserApi.Controllers;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocumentParserApi.Services
{
    // ============================
    // IIntentClassifier.cs
    // ============================
    public interface IIntentClassifier
    {
        Task<IntentResult> ClassifyAsync(string userMessage);
    }

    // ============================
    // IntentClassifier.cs
    // ============================
    public class IntentClassifier : IIntentClassifier
    {
        private readonly OpenAIClient _client;
        private readonly IConfiguration _config;

        public IntentClassifier(OpenAIClient client, IConfiguration config)
        {
            _client = client;
            _config = config;
        }

        public async Task<IntentResult> ClassifyAsync(string userMessage)
        {
            var instructionPath = Path.Combine(Directory.GetCurrentDirectory(), "instructions", "intent_instruction.txt");
            var instruction = await File.ReadAllTextAsync(instructionPath);

            var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, instruction),
            new ChatMessage(ChatRole.User, userMessage)
        };

            var chatCompletionsOptions = new ChatCompletionsOptions();
            foreach (var msg in messages) chatCompletionsOptions.Messages.Add(msg);

            var response = await _client.GetChatCompletionsAsync(
                deploymentOrModelName: _config["AzureOpenAI:DeploymentName"],
                chatCompletionsOptions);

            var content = response.Value.Choices[0].Message.Content;
            return JsonSerializer.Deserialize<IntentResult>(content);
        }
    }

    // ============================
    // IRouteMappingService.cs
    // ============================
    public interface IRouteMappingService
    {
        string GetRouteForForm(string formName);
    }

    public class RouteMappingService : IRouteMappingService
    {
        private readonly Dictionary<string, string> _formRoutes = new()
        {
           
        };

        public string GetRouteForForm(string formName)
        {
            return _formRoutes.TryGetValue(formName, out var route) ? route : "/";
        }
    }

}
