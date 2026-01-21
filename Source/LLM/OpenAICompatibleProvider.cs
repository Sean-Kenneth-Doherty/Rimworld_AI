using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AdvancedColonistIntelligence.LLM
{
    /// <summary>
    /// Provider for OpenAI-compatible APIs (OpenAI, Grok, LM Studio, etc.)
    /// </summary>
    public class OpenAICompatibleProvider : ILLMProvider
    {
        public string Name { get; }
        private readonly string endpoint;
        private readonly string apiKey;
        private readonly string model;

        public OpenAICompatibleProvider(string name, string endpoint, string apiKey, string model)
        {
            Name = name;
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
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Timeout = 60000; // 60 second timeout

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
                Log.Error($"[ACI] LLM request failed: {errorMessage}");
                return LLMResponse.Failed(errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error($"[ACI] LLM request exception: {ex.Message}");
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
            // Manual JSON building to avoid dependencies
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{EscapeJson(model)}\",");
            sb.Append("\"messages\":[");
            sb.Append($"{{\"role\":\"system\",\"content\":\"{EscapeJson(systemPrompt)}\"}},");
            sb.Append($"{{\"role\":\"user\",\"content\":\"{EscapeJson(userPrompt)}\"}}");
            sb.Append("],");
            sb.Append($"\"max_tokens\":{maxTokens},");
            sb.Append("\"temperature\":0.7");
            sb.Append("}");
            return sb.ToString();
        }

        private string ParseResponse(string json, out int tokensUsed)
        {
            tokensUsed = 0;

            // Simple JSON parsing without external dependencies
            // Looking for: "content": "..." in choices[0].message
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
            content = UnescapeJson(content);

            // Try to parse token usage
            var usageMarker = "\"total_tokens\":";
            var usageStart = json.IndexOf(usageMarker);
            if (usageStart != -1)
            {
                usageStart += usageMarker.Length;
                var usageEnd = usageStart;
                while (usageEnd < json.Length && char.IsDigit(json[usageEnd]))
                    usageEnd++;
                if (int.TryParse(json.Substring(usageStart, usageEnd - usageStart), out int tokens))
                    tokensUsed = tokens;
            }

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
