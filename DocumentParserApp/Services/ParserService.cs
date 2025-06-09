using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocumentParserApi.Services
{
    public interface IParserService
    {
        Task<JsonDocument> GenerateStructuredJsonAsync(string inputText, string instructionKey);
    }

    public class ParserService : IParserService
    {
        private readonly OpenAIClient _openAIClient;
        private readonly string _deploymentName;

        public ParserService(IConfiguration configuration)
        {
            var endpoint = new Uri(configuration["AzureOpenAI:Endpoint"]);
            var key = new AzureKeyCredential(configuration["AzureOpenAI:Key"]);
            _deploymentName = configuration["AzureOpenAI:DeploymentName"];
            _openAIClient = new OpenAIClient(endpoint, key);
        }

        public async Task<JsonDocument> GenerateStructuredJsonAsync(string inputText, string instructionKey)
        {
            // Load instruction from file or other source
            var systemPrompt = await InstructionLoader.GetInstructionsAsync(instructionKey);

            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, systemPrompt),
                new ChatMessage(ChatRole.User, inputText)
            };

            var chatOptions = new ChatCompletionsOptions()
            {
                Messages = { messages[0], messages[1] },
                Temperature = 0,
                MaxTokens = 500,
            };

            var response = await _openAIClient.GetChatCompletionsAsync(_deploymentName, chatOptions);
            var completion = response.Value.Choices[0].Message.Content;

            try
            {
                return JsonDocument.Parse(completion);
            }
            catch
            {
                var errorJson = JsonDocument.Parse(
                    $"{{\"error\": \"Invalid JSON from OpenAI\", \"rawResponse\": {JsonSerializer.Serialize(completion)}}}");
                return errorJson;
            }
        }
    }
}
