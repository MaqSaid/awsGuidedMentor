// GuidedMentor Seed Data Generator
// Populates the platform with realistic simulation data for demo and testing purposes.
// Usage: dotnet run --project tools/SeedData -- --environment dev

using System.CommandLine;
using System.CommandLine.Invocation;
using Amazon.DynamoDBv2;
using GuidedMentor.Tools.SeedData;

var environmentOption = new Option<string>(
    name: "--environment",
    description: "Target environment for seed data (e.g., dev, staging). Production is not allowed.")
{
    IsRequired = true
};

var rootCommand = new RootCommand("GuidedMentor Seed Data Generator — populates DynamoDB with realistic demo data")
{
    environmentOption
};

rootCommand.SetHandler(async (InvocationContext context) =>
{
    var environment = context.ParseResult.GetValueForOption(environmentOption)!;

    Console.WriteLine("GuidedMentor Seed Data Generator");
    Console.WriteLine("================================");
    Console.WriteLine($"Target environment: {environment}");
    Console.WriteLine();

    try
    {
        using var dynamoDbClient = new AmazonDynamoDBClient();
        var generator = new SeedDataGenerator(dynamoDbClient);

        await generator.SeedAsync(environment);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot seed production"))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"ERROR: {ex.Message}");
        Console.ResetColor();
        context.ExitCode = 1;
    }
});

return await rootCommand.InvokeAsync(args);
