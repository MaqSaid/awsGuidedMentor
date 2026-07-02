using System.Net;
using System.Text.Json;
using GuidedMentor.Engagement.Application.Services;
using GuidedMentor.Engagement.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GuidedMentor.Engagement.Tests.Services;

/// <summary>
/// Unit tests for HuggingFaceIntentClassifier.
/// Validates graceful degradation, confidence thresholds, and correct label mapping.
/// </summary>
public sealed class HuggingFaceIntentClassifierTests
{
    private readonly HuggingFaceOptions _options = new()
    {
        ApiKey = "test-key",
        ClassificationModel = "facebook/bart-large-mnli"
    };

    private HuggingFaceIntentClassifier CreateClassifier(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(_options);
        return new HuggingFaceIntentClassifier(httpClient, options, NullLogger<HuggingFaceIntentClassifier>.Instance);
    }

    [Fact]
    public async Task ClassifyAsync_PlatformHelpWithHighConfidence_ReturnsPlatformHelp()
    {
        // Arrange
        var response = new HfClassificationResponse
        {
            Labels = ["platform help and troubleshooting", "navigation and finding pages", "off-topic unrelated question"],
            Scores = [0.85, 0.10, 0.05]
        };
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        var classifier = CreateClassifier(handler);

        // Act
        var result = await classifier.ClassifyAsync("How do I reset my password?");

        // Assert
        result.Should().Be(ChatIntent.PlatformHelp);
    }

    [Fact]
    public async Task ClassifyAsync_OffTopicWithHighConfidence_ReturnsOffTopic()
    {
        // Arrange
        var response = new HfClassificationResponse
        {
            Labels = ["off-topic unrelated question", "platform help and troubleshooting", "navigation and finding pages"],
            Scores = [0.90, 0.06, 0.04]
        };
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        var classifier = CreateClassifier(handler);

        // Act
        var result = await classifier.ClassifyAsync("What is the weather today?");

        // Assert
        result.Should().Be(ChatIntent.OffTopic);
    }

    [Fact]
    public async Task ClassifyAsync_NavigationWithHighConfidence_ReturnsNavigation()
    {
        // Arrange
        var response = new HfClassificationResponse
        {
            Labels = ["navigation and finding pages", "platform help and troubleshooting", "off-topic unrelated question"],
            Scores = [0.82, 0.12, 0.06]
        };
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        var classifier = CreateClassifier(handler);

        // Act
        var result = await classifier.ClassifyAsync("Where is the settings page?");

        // Assert
        result.Should().Be(ChatIntent.Navigation);
    }

    [Fact]
    public async Task ClassifyAsync_ConfidenceBelowThreshold_ReturnsPlatformHelp()
    {
        // Arrange — top score is 0.65 (below 0.7 threshold)
        var response = new HfClassificationResponse
        {
            Labels = ["off-topic unrelated question", "platform help and troubleshooting", "navigation and finding pages"],
            Scores = [0.65, 0.20, 0.15]
        };
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        var classifier = CreateClassifier(handler);

        // Act
        var result = await classifier.ClassifyAsync("Can you help me with something?");

        // Assert — falls back to PlatformHelp when confidence is too low
        result.Should().Be(ChatIntent.PlatformHelp);
    }

    [Fact]
    public async Task ClassifyAsync_ApiReturnsError_ReturnsPlatformHelp_GracefulDegradation()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "Service Unavailable");
        var classifier = CreateClassifier(handler);

        // Act
        var result = await classifier.ClassifyAsync("How do I use the platform?");

        // Assert — graceful degradation: never block the user
        result.Should().Be(ChatIntent.PlatformHelp);
    }

    [Fact]
    public async Task ClassifyAsync_ApiThrowsException_ReturnsPlatformHelp_GracefulDegradation()
    {
        // Arrange
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var classifier = CreateClassifier(handler);

        // Act
        var result = await classifier.ClassifyAsync("Tell me about sessions");

        // Assert
        result.Should().Be(ChatIntent.PlatformHelp);
    }

    [Fact]
    public async Task ClassifyAsync_ApiReturnsEmptyResponse_ReturnsPlatformHelp()
    {
        // Arrange
        var response = new HfClassificationResponse
        {
            Labels = [],
            Scores = []
        };
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        var classifier = CreateClassifier(handler);

        // Act
        var result = await classifier.ClassifyAsync("Random question");

        // Assert
        result.Should().Be(ChatIntent.PlatformHelp);
    }

    [Fact]
    public async Task ClassifyAsync_ApiReturnsNullBody_ReturnsPlatformHelp()
    {
        // Arrange — null JSON response body
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "null");
        var classifier = CreateClassifier(handler);

        // Act
        var result = await classifier.ClassifyAsync("Something");

        // Assert
        result.Should().Be(ChatIntent.PlatformHelp);
    }

    [Fact]
    public async Task ClassifyAsync_TimeoutException_ReturnsPlatformHelp()
    {
        // Arrange
        var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("Request timed out"));
        var classifier = CreateClassifier(handler);

        // Act
        var result = await classifier.ClassifyAsync("How do I browse mentors?");

        // Assert
        result.Should().Be(ChatIntent.PlatformHelp);
    }

    /// <summary>
    /// Fake HttpMessageHandler that returns a pre-configured response.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// HttpMessageHandler that throws an exception to simulate network failures.
    /// </summary>
    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }
}
