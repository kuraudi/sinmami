using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;

namespace RentGen.Infrastructure.Llm.DeepSeek;

public sealed class DeepSeekClient(
    HttpClient httpClient,
    IOptions<DeepSeekOptions> options,
    ILogger<DeepSeekClient> logger) : IGenerativeAiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient;
    private readonly DeepSeekOptions _options = options.Value;
    private readonly ILogger<DeepSeekClient> _logger = logger;

    public async Task<AiCompletionResult> GenerateAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogInformation("DeepSeek API key is not configured. Returning stub response for task {TaskType}.", request.TaskType);
            return new AiCompletionResult(
                $"[LLM stub:{request.TaskType}] {request.UserPrompt[..Math.Min(request.UserPrompt.Length, 120)]}",
                _options.Model);
        }

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        var payload = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            },
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stream = false
        };

        var attempts = Math.Max(1, _options.MaxRetries + 1);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json");

            _logger.LogInformation(
                "Calling DeepSeek for task {TaskType} with model {Model}. Attempt {Attempt}/{Attempts}.",
                request.TaskType,
                _options.Model,
                attempt,
                attempts);

            HttpResponseMessage response;
            string responseBody;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                throw new HttpRequestException(
                    $"DeepSeek request timed out after {_options.TimeoutSeconds} seconds for task '{request.TaskType}'.",
                    exception);
            }

            if (response.IsSuccessStatusCode)
            {
                using (response)
                {
                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;
                var contentText = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
                var model = root.TryGetProperty("model", out var modelNode) ? modelNode.GetString() : _options.Model;

                _logger.LogInformation(
                    "DeepSeek call succeeded for task {TaskType} with model {Model}.",
                    request.TaskType,
                    model ?? _options.Model);

                return new AiCompletionResult(contentText, model);
                }
            }

            var shouldRetry = attempt < attempts && IsTransient(response.StatusCode);
            _logger.LogWarning(
                "DeepSeek call failed for task {TaskType}. Status {StatusCode}. Retry {ShouldRetry}.",
                request.TaskType,
                (int)response.StatusCode,
                shouldRetry);

            if (!shouldRetry)
            {
                var safeBody = responseBody.Length > 500 ? responseBody[..500] : responseBody;
                response.Dispose();
                throw new HttpRequestException($"DeepSeek request failed with status {(int)response.StatusCode}: {safeBody}");
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), cancellationToken);
        }

        throw new InvalidOperationException("DeepSeek request loop exited unexpectedly.");
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500;
    }
}
