using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FsCheck.Fluent;
using NSubstitute;

namespace GuidedMentor.Tools.SeedData.Tests.Properties;

[Trait("Category", "Property")]
public sealed class SeedDataPropertyTests
{
    [Property(MaxTest = 100)]
    public FsCheck.Property Property35_SeedDataGeneratorIsIdempotent()
    {
        return Prop.ForAll(Gen.Choose(1, 5).ToArbitrary(), executionCount =>
        {
            var writtenItems = new List<Dictionary<string, AttributeValue>>();
            var seedMarkerWritten = false;

            var mockDynamoDb = Substitute.For<IAmazonDynamoDB>();

            mockDynamoDb.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    if (seedMarkerWritten)
                        return new GetItemResponse
                        {
                            Item = new Dictionary<string, AttributeValue>
                            {
                                ["pk"] = new AttributeValue { S = "SEED_MARKER" },
                                ["sk"] = new AttributeValue { S = "v1" }
                            }
                        };
                    return new GetItemResponse { Item = new Dictionary<string, AttributeValue>() };
                });

            mockDynamoDb.PutItemAsync(Arg.Any<PutItemRequest>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var request = callInfo.Arg<PutItemRequest>();
                    writtenItems.Add(request.Item);
                    if (request.Item.TryGetValue("pk", out var pk) && pk.S == "SEED_MARKER")
                        seedMarkerWritten = true;
                    return new PutItemResponse();
                });

            mockDynamoDb.BatchWriteItemAsync(Arg.Any<BatchWriteItemRequest>(), Arg.Any<CancellationToken>())
                .Returns(new BatchWriteItemResponse());

            var sut = new SeedDataGenerator(mockDynamoDb);
            var firstRunWriteCount = 0;

            for (var i = 0; i < executionCount; i++)
            {
                writtenItems.Clear();
                sut.SeedAsync("dev").GetAwaiter().GetResult();

                if (i == 0)
                    firstRunWriteCount = writtenItems.Count;
                else
                    writtenItems.Should().BeEmpty("subsequent executions should not write data when seed marker exists");
            }

            firstRunWriteCount.Should().BeGreaterThan(0, "first execution should write seed data");
        });
    }
}
