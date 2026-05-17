using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentGen.Application.Common.Models;
using RentGen.Infrastructure.Llm.DeepSeek;

namespace RentGen.UnitTests;

public class DeepSeekClientTests
{
    [Fact]
    public async Task GenerateAsync_WithoutApiKey_ShouldReturnStubResponse()
    {
        var client = new DeepSeekClient(
            new HttpClient(),
            Options.Create(new DeepSeekOptions
            {
                ApiKey = string.Empty,
                Model = "deepseek-chat"
            }),
            new TestLogger<DeepSeekClient>());

        var result = await client.GenerateAsync(
            new AiCompletionRequest("draft_help", "system", "user prompt for testing"),
            CancellationToken.None);

        Assert.Contains("[LLM stub:draft_help]", result.Content);
        Assert.Equal("deepseek-chat", result.Model);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
