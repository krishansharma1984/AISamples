using Azure;
using Azure.AI.OpenAI;
using DocumentParserApi.Services;
using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.AI.OpenAI;
using DocumentParserApi.Services;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Http.Cors;
using System.Net;
using System.IO;
using DocumentParserAPI.Controllers;
namespace DocumentParserApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiKeyAuth]
    public class WorkflowController : ControllerBase
    {
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;
        private readonly string _apiKey;
        private readonly IIntentClassifier _intentClassifier;
        private readonly IRouteMappingService _routeMappingService;
        private readonly IParserService _parserService;

        public WorkflowController(IParserService parserService,IConfiguration configuration, IIntentClassifier intentClassifier, IRouteMappingService routeMappingService)
        {
            var endpoint = new Uri(configuration["AzureOpenAI:Endpoint"]);
            var key = new AzureKeyCredential(configuration["AzureOpenAI:Key"]);
            _deploymentName = configuration["AzureOpenAI:DeploymentName"];
            _apiKey = configuration["ApiKey"];

            _client = new OpenAIClient(endpoint, key);
            _intentClassifier = intentClassifier;
            _routeMappingService = routeMappingService;
            _parserService = parserService;

        }

        [HttpPost("handle")]
        public async Task<IActionResult> HandleUserRequest([FromBody] UserRequestDto userRequest)
        {
            var classification = await  _intentClassifier.ClassifyAsync(userRequest.UserMessage);

            switch (classification.Intent)
            {
                case "generate_steps":
                    return await GenerateWorkflowHtml(classification.ProcessType);
                case "parse_text":
                case "parse_pdf":
                    var jsonDoc = await _parserService.GenerateStructuredJsonAsync(userRequest.UserMessage,classification.ProcessType.ToLower());
                    return Content(jsonDoc.RootElement.GetRawText(), "application/json");
                case "open_form":
                    var route = _routeMappingService.GetRouteForForm(classification.ProcessType);
                    return Ok(new { redirectUrl = route });
                default:
                    return Ok(new { message = "Sorry, I couldn't understand the request." });
            }
        }


        [HttpGet("process-steps")]
        public async Task<IActionResult> GenerateWorkflowHtml([FromQuery] string processName)
        {
            var knowledgeText = await InstructionLoader.GetKnowledgeBase(processName);

            var instructionText = await InstructionLoader.GetProcessInstructions(processName);

        
            var chatOptions = new ChatCompletionsOptions()
            {
                MaxTokens = 1000,
                Temperature = 0.3f,
            };

            chatOptions.Messages.Add(new ChatMessage(ChatRole.System, instructionText));
            chatOptions.Messages.Add(new ChatMessage(ChatRole.User, knowledgeText));

            Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(
                _deploymentName,
                chatOptions
            );
            var chatCompletion = response.Value.Choices[0].Message.Content.Trim();

            try
            {
                var workflowData = JsonSerializer.Deserialize<WorkflowResponse>(chatCompletion, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (workflowData?.Steps == null || workflowData.Steps.Count == 0)
                    return BadRequest("No steps found in AI response.");

                var htmlBuilder = new StringBuilder();
                htmlBuilder.Append("<ol class='list-group list-group-numbered'>");
                foreach (var step in workflowData.Steps)
                {
                    htmlBuilder.Append($@"
                    <li class='list-group-item'>
                      <h5 class='mb-1'>Step {step.StepNumber}: {WebUtility.HtmlEncode(step.Title)}</h5>
                      <p class='mb-2'>{WebUtility.HtmlEncode(step.Description)}</p>
                      <a href='#' onclick='app.navigateTo(""{WebUtility.HtmlEncode(step.Route)}"")' class='btn btn-primary'>Open Form</a>
                    </li>");
                }

                htmlBuilder.Append("</ol>");
                return Content(htmlBuilder.ToString(), "text/html");
            }
            catch (JsonException)
            {
                return BadRequest("Failed to parse AI response JSON.");
            }
        }
    }

    public class WorkflowResponse
    {
        public List<WorkflowStep> Steps { get; set; }
    }

    public class WorkflowStep
    {
        public int StepNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Route { get; set; }
    }
    public class UserRequestDto
    {
        public string UserMessage { get; set; }           // For natural language input (e.g., "I want to onboard someone")
        public string Text { get; set; }                  // Optional: if raw text to parse
        public IFormFile File { get; set; }               // Optional: if PDF or other file is uploaded
        public byte[] AudioBytes { get; set; }            // Optional: for voice input
    }
    // ============================
    // IntentResult.cs
    // ============================
    public class IntentResult
    {
        public string Intent { get; set; }
        public string ProcessType { get; set; }
    }
}
