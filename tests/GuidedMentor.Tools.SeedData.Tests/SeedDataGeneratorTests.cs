using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GuidedMentor.Tools.SeedData.Tests;

public sealed class SeedDataGeneratorTests
{
    private readonly IAmazonDynamoDB _mockDynamoDb = Substitute.For<IAmazonDynamoDB>();
    private readonly SeedDataGenerator _sut;

    public SeedDataGeneratorTests()
    {
        _sut = new SeedDataGenerator(_mockDynamoDb);
    }

    [Theory]
    [InlineData("prod")]
    [InlineData("production")]
    [InlineData("PROD")]
    [InlineData("PRODUCTION")]
    [InlineData("Prod")]
    [InlineData("Production")]
    [InlineData(" prod ")]
    [InlineData(" production ")]
    public void RejectProductionEnvironment_ThrowsForProductionValues(string environment)
    {
        var act = () => SeedDataGenerator.RejectProductionEnvironment(environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot seed production environment*");
    }

    [Theory]
    [InlineData("dev")]
    [InlineData("staging")]
    [InlineData("demo")]
    [InlineData("local")]
    [InlineData("test")]
    [InlineData("DEV")]
    public void RejectProductionEnvironment_AllowsNonProductionValues(string environment)
    {
        var act = () => SeedDataGenerator.RejectProductionEnvironment(environment);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task SeedAsync_ThrowsForProductionEnvironment()
    {
        var act = () => _sut.SeedAsync("prod");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot seed production environment*");
    }

    [Fact]
    public async Task SeedAsync_SkipsWhenSeedMarkerExists()
    {
        // Arrange: seed marker already exists
        _mockDynamoDb.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    ["pk"] = new AttributeValue { S = "SEED_MARKER" },
                    ["sk"] = new AttributeValue { S = "v1" },
                    ["seededAt"] = new AttributeValue { S = "2024-01-01T00:00:00Z" }
                }
            });

        // Act
        await _sut.SeedAsync("dev");

        // Assert: PutItem should NOT have been called (no data was written)
        await _mockDynamoDb.DidNotReceive()
            .PutItemAsync(Arg.Any<PutItemRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_ProceedsWhenNoSeedMarker()
    {
        // Arrange: no seed marker (empty response)
        _mockDynamoDb.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>()
            });

        // Act
        await _sut.SeedAsync("dev");

        // Assert: PutItem should have been called to write the seed marker
        await _mockDynamoDb.Received(1)
            .PutItemAsync(Arg.Is<PutItemRequest>(r =>
                r.Item["pk"].S == "SEED_MARKER" &&
                r.Item["sk"].S == "v1" &&
                r.Item["environment"].S == "dev"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedMarkerExistsAsync_ReturnsFalseWhenTableNotFound()
    {
        // Arrange: table doesn't exist
        _mockDynamoDb.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException("Table not found"));

        // Act
        var result = await _sut.SeedMarkerExistsAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SeedMarkerExistsAsync_ReturnsTrueWhenMarkerExists()
    {
        // Arrange
        _mockDynamoDb.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    ["pk"] = new AttributeValue { S = "SEED_MARKER" },
                    ["sk"] = new AttributeValue { S = "v1" }
                }
            });

        // Act
        var result = await _sut.SeedMarkerExistsAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SeedMarkerExistsAsync_ReturnsFalseWhenItemIsEmpty()
    {
        // Arrange
        _mockDynamoDb.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>()
            });

        // Act
        var result = await _sut.SeedMarkerExistsAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ThrowsWhenDynamoDbClientIsNull()
    {
        var act = () => new SeedDataGenerator(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dynamoDbClient");
    }
}
