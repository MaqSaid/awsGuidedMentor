using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GuidedMentor.SharedInfrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GuidedMentor.SharedInfrastructure.Tests.HealthChecks;

public sealed class DynamoDbHealthCheckTests
{
    private readonly IAmazonDynamoDB _dynamoDb = Substitute.For<IAmazonDynamoDB>();
    private readonly ILogger<DynamoDbHealthCheck> _logger = Substitute.For<ILogger<DynamoDbHealthCheck>>();
    private const string TableName = "TestTable";

    private DynamoDbHealthCheck CreateSut() => new(_dynamoDb, TableName, _logger);

    [Fact]
    public async Task CheckHealthAsync_WhenTableActive_ReturnsHealthy()
    {
        // Arrange
        _dynamoDb.DescribeTableAsync(Arg.Any<DescribeTableRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DescribeTableResponse
            {
                Table = new TableDescription { TableStatus = TableStatus.ACTIVE }
            });

        var sut = CreateSut();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("active", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTableUpdating_ReturnsDegraded()
    {
        // Arrange
        _dynamoDb.DescribeTableAsync(Arg.Any<DescribeTableRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DescribeTableResponse
            {
                Table = new TableDescription { TableStatus = TableStatus.UPDATING }
            });

        var sut = CreateSut();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTableNotFound_ReturnsUnhealthy()
    {
        // Arrange
        _dynamoDb.DescribeTableAsync(Arg.Any<DescribeTableRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ResourceNotFoundException("Table not found"));

        var sut = CreateSut();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("does not exist", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _dynamoDb.DescribeTableAsync(Arg.Any<DescribeTableRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonDynamoDBException("Connection refused"));

        var sut = CreateSut();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
