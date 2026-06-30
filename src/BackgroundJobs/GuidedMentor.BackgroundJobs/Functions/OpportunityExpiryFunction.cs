using Amazon.Lambda.Core;
using GuidedMentor.Mentoring.Application.Commands.Opportunities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Lambda function triggered daily by EventBridge Scheduler.
/// Archives expired opportunity postings and notifies mentors.
/// Jobs get a renewal option; workshops/events/training are auto-archived after their event date.
///
/// Requirements: 20.7 (scheduler for recurring jobs)
/// </summary>
public sealed class OpportunityExpiryFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<OpportunityExpiryFunction> _logger;

    public OpportunityExpiryFunction()
    {
        var services = ServiceProviderFactory.Create();
        _mediator = services.GetRequiredService<IMediator>();
        _logger = services.GetRequiredService<ILogger<OpportunityExpiryFunction>>();
    }

    /// <summary>
    /// Entry point invoked daily by EventBridge Scheduler.
    /// Dispatches ProcessOpportunityExpiryCommand via MediatR.
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task HandleAsync(ScheduledEvent scheduledEvent, ILambdaContext context)
    {
        _logger.LogInformation(
            "Opportunity expiry job triggered. RequestId: {RequestId}",
            context.AwsRequestId);

        try
        {
            var command = new ProcessOpportunityExpiryCommand();
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Opportunity expiry processing completed successfully.");
            }
            else
            {
                _logger.LogWarning("Opportunity expiry processing completed with error: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Opportunity expiry processing failed.");
            throw;
        }
    }
}
