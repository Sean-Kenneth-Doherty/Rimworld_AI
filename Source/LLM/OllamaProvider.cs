using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AdvancedColonistIntelligence.LLM
{
    /// <summary>
    /// Provider for local Ollama API.
    /// </summary>
    public class OllamaProvider : ILLMProvider
    {
        public string Name => "Ollama";
        private readonly string endpoint;
        private readonly string model;

        public OllamaProvider(string endpoint, string model)
        {
            this.endpoint = endpoint;
            this.model = model;
        }

        public async Task<LLMResponse> SendRequestAsync(string systemPrompt, string userPrompt, int maxTokens)
        {
            var startTime = DateTime.Now;

            try
            {
                var requestBody = BuildRequestBody(systemPrompt, userPrompt);

                var request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 120000; // 2 minute timeout for local models

                using (var streamWriter = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    await streamWriter.WriteAsync(requestBody);
                }

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseText = await streamReader.ReadToEndAsync();
                    var content = ParseResponse(responseText);

                    var elapsed = (float)(DateTime.Now - startTime).TotalMilliseconds;

                    return new LLMResponse
                    {
                        Success = true,
                        Content = content,
                        TokensUsed = 0, // Ollama doesn't report tokens in same way
                        ResponseTimeMs = elapsed
                    };
                }
            }
            catch (WebException ex)
            {
                var errorMessage = "Connection failed - is Ollama running?";
                Log.Error($"[ACI] Ollama request failed: {ex.Message}");
                return LLMResponse.Failed(errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error($"[ACI] Ollama request exception: {ex.Message}");
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

        private string BuildRequestBody(string systemPrompt, string userPrompt)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{EscapeJson(model)}\",");
            sb.Append("\"messages\":[");
            sb.Append($"{{\"role\":\"system\",\"content\":\"{EscapeJson(systemPrompt)}\"}},");
            sb.Append($"{{\"role\":\"user\",\"content\":\"{EscapeJson(userPrompt)}\"}}");
            sb.Append("],");
            sb.Append("\"stream\":false");
            sb.Append("}");
            return sb.ToString();
        }

        private string ParseResponse(string json)
        {
            // Ollama returns: {"message":{"role":"assistant","content":"..."},...}
            var contentMarker = "\"content\":";
            var contentStart = json.LastIndexOf(contentMarker);

            if (contentStart == -1)
                return null;

            contentStart += contentMarker.Length;

            // Skip whitespace and opening quote
            while (contentStart < json.Length && (json[contentStart] == ' ' || json[contentStart] == '"'))
                contentStart++;

            // Find the end of the content string
            var contentEnd = contentStart;
            var escaped = false;
            while (contentEnd < json.Length)
            {
                if (escaped)
                {
                    escaped = false;
                    contentEnd++;
                    continue;
                }
                if (json[contentEnd] == '\\')
                {
                    escaped = true;
                    contentEnd++;
                    continue;
                }
                if (json[contentEnd] == '"')
                    break;
                contentEnd++;
            }

            var content = json.Substring(contentStart, contentEnd - contentStart);
            return UnescapeJson(content);
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
