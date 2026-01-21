using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AdvancedColonistIntelligence.LLM
{
    /// <summary>
    /// Provider for Anthropic Claude API (different format from OpenAI).
    /// </summary>
    public class ClaudeProvider : ILLMProvider
    {
        public string Name => "Claude";
        private readonly string endpoint;
        private readonly string apiKey;
        private readonly string model;

        public ClaudeProvider(string endpoint, string apiKey, string model)
        {
            this.endpoint = endpoint;
            this.apiKey = apiKey;
            this.model = model;
        }

        public async Task<LLMResponse> SendRequestAsync(string systemPrompt, string userPrompt, int maxTokens)
        {
            var startTime = DateTime.Now;

            try
            {
                var requestBody = BuildRequestBody(systemPrompt, userPrompt, maxTokens);

                var request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Timeout = 60000;

                using (var streamWriter = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    await streamWriter.WriteAsync(requestBody);
                }

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseText = await streamReader.ReadToEndAsync();
                    var content = ParseResponse(responseText, out int tokensUsed);

                    var elapsed = (float)(DateTime.Now - startTime).TotalMilliseconds;

                    return new LLMResponse
                    {
                        Success = true,
                        Content = content,
                        TokensUsed = tokensUsed,
                        ResponseTimeMs = elapsed
                    };
                }
            }
            catch (WebException ex)
            {
                var errorMessage = "Connection failed";
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        errorMessage = await reader.ReadToEndAsync();
                    }
                }
                Log.Error($"[ACI] Claude request failed: {errorMessage}");
                return LLMResponse.Failed(errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error($"[ACI] Claude request exception: {ex.Message}");
                return LLMResponse.Failed(ex.Message);
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            var response = await SendRequestAsync(
                "You are a test assistant.",
                "Reply with only the word 'connected'.",
                10
            );
            return response.Success;
        }

        private string BuildRequestBody(string systemPrompt, string userPrompt, int maxTokens)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{EscapeJson(model)}\",");
            sb.Append($"\"max_tokens\":{maxTokens},");
            sb.Append($"\"system\":\"{EscapeJson(systemPrompt)}\",");
            sb.Append("\"messages\":[");
            sb.Append($"{{\"role\":\"user\",\"content\":\"{EscapeJson(userPrompt)}\"}}");
            sb.Append("]");
            sb.Append("}");
            return sb.ToString();
        }

        private string ParseResponse(string json, out int tokensUsed)
        {
            tokensUsed = 0;

            // Claude returns: {"content":[{"type":"text","text":"..."}],...}
            var textMarker = "\"text\":";
            var textStart = json.IndexOf(textMarker);

            if (textStart == -1)
                return null;

            textStart += textMarker.Length;

            // Skip whitespace and opening quote
            while (textStart < json.Length && (json[textStart] == ' ' || json[textStart] == '"'))
                textStart++;

            // Find the end of the text string
            var textEnd = textStart;
            var escaped = false;
            while (textEnd < json.Length)
            {
                if (escaped)
                {
                    escaped = false;
                    textEnd++;
                    continue;
                }
                if (json[textEnd] == '\\')
                {
                    escaped = true;
                    textEnd++;
                    continue;
                }
                if (json[textEnd] == '"')
                    break;
                textEnd++;
            }

            var content = json.Substring(textStart, textEnd - textStart);
            content = UnescapeJson(content);

            // Parse token usage from Claude's format
            var inputTokensMarker = "\"input_tokens\":";
            var outputTokensMarker = "\"output_tokens\":";

            int inputTokens = 0, outputTokens = 0;

            var inputStart = json.IndexOf(inputTokensMarker);
            if (inputStart != -1)
            {
                inputStart += inputTokensMarker.Length;
                var inputEnd = inputStart;
                while (inputEnd < json.Length && char.IsDigit(json[inputEnd]))
                    inputEnd++;
                int.TryParse(json.Substring(inputStart, inputEnd - inputStart), out inputTokens);
            }

            var outputStart = json.IndexOf(outputTokensMarker);
            if (outputStart != -1)
            {
                outputStart += outputTokensMarker.Length;
                var outputEnd = outputStart;
                while (outputEnd < json.Length && char.IsDigit(json[outputEnd]))
                    outputEnd++;
                int.TryParse(json.Substring(outputStart, outputEnd - outputStart), out outputTokens);
            }

            tokensUsed = inputTokens + outputTokens;

            return content;
        }

        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private string UnescapeJson(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
    }
}
