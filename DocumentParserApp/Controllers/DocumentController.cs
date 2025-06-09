using DocumentParserApi.Services;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocumentParserAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiKeyAuth]
    public class DocumentController : ControllerBase
    {
        private readonly IParserService _parserService;
        private readonly string _apiKey;
        private readonly string speechKey;
        private readonly string speechRegion;

        public DocumentController(IParserService parserService, IConfiguration configuration)
        {
            _parserService = parserService;
            _apiKey = configuration["ApiKey"];
            speechKey = configuration["AzureSpeech:Key"];
            speechRegion = configuration["AzureSpeech:Region"];
        }

        [HttpPost("parse-pdf")]
        public async Task<IActionResult> ParsePdf([FromForm] IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return BadRequest("No PDF file uploaded.");

            string extractedText;
            using (var stream = pdfFile.OpenReadStream())
            using (var pdfReader = new PdfReader(stream))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                var textBuilder = new StringBuilder();
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var pageText = PdfTextExtractor.GetTextFromPage(page);
                    textBuilder.AppendLine(pageText);
                }
                extractedText = textBuilder.ToString();
            }

            var jsonResult = await _parserService.GenerateStructuredJsonAsync(extractedText, "ContractRequest");

            return Ok(jsonResult);
        }

        [HttpPost("parse-text")]
        public async Task<IActionResult> ParseText([FromBody] TextInput input)
        {
            if (string.IsNullOrWhiteSpace(input?.Text))
                return BadRequest("Text input is empty.");

            var jsonResult = await _parserService.GenerateStructuredJsonAsync(input.Text, "ContractRequest");

            return Ok(jsonResult);
        }

        [HttpPost("parse-audio")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> ParseAudio([FromForm] IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
                return BadRequest("No audio file uploaded.");

            var config = SpeechConfig.FromSubscription(speechKey, speechRegion);
            config.SpeechRecognitionLanguage = "en-US";

            string recognizedText;
            using var audioStream = audioFile.OpenReadStream();
            using var audioInput = AudioConfig.FromStreamInput(new BinaryAudioStreamReader(audioStream));
            using var recognizer = new SpeechRecognizer(config, audioInput);

            var result = await recognizer.RecognizeOnceAsync();
            if (result.Reason != ResultReason.RecognizedSpeech)
                return BadRequest($"Speech recognition failed: {result.Reason}");

            recognizedText = result.Text;

            var jsonResult = await _parserService.GenerateStructuredJsonAsync(recognizedText, "ContractRequest");
            return Ok(jsonResult);
        }
    }

    public class TextInput
    {
        public string Text { get; set; }
    }
}
