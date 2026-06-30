using GuidedMentor.Observability.Logging;
using GuidedMentor.SharedInfrastructure.AuditLogging;
using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps all command handlers with audit logging.
/// Only triggers for requests implementing <see cref="IAuditableCommand"/>.
/// Records: who (userId from JWT), when (UTC timestamp), what (command type),
/// which resource (extracted from command), and correlationId.
/// For Super Admin operations (implementing <see cref="IAdminCommand"/>),
/// additionally logs adminId, target, and reason.
/// </summary>
public sealed class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ILogger<AuditLoggingBehavior<TRequest, TResponse>> _logger;

    public AuditLoggingBehavior(
        IAuditLogWriter auditLogWriter,
        ILogger<AuditLoggingBehavior<TRequest, TResponse>> logger)
    {
        _auditLogWriter = auditLogWriter;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only audit commands that implement IAuditableCommand
        if (request is not IAuditableCommand auditableCommand)
        {
            return await next();
        }

        var commandName = typeof(TRequest).Name;
        var correlationId = CorrelationContext.CurrentCorrelationId ?? Guid.NewGuid().ToString();
        var success = true;

        try
        {
            var response = await next();

            // Check if the result indicates failure (for Result/Result<T> return types)
            success = IsSuccessResult(response);

            return response;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogWarning(
                ex,
                "Audit: Command {CommandName} failed with exception for user {UserId}",
                commandName,
                auditableCommand.UserId);
            throw;
        }
        finally
        {
            var record = BuildAuditRecord(auditableCommand, commandName, correlationId, success);

            try
            {
                await _auditLogWriter.WriteAsync(record, cancellationToken);
            }
            catch (Exception ex)
            {
                // Audit logging should never cause a command to fail
                _logger.LogError(
                    ex,
                    "Failed to write audit log for {CommandName} by user {UserId}",
                    commandName,
                    auditableCommand.UserId);
            }
        }
    }

    private static AuditLogRecord BuildAuditRecord(
        IAuditableCommand command,
        string commandName,
        string correlationId,
        bool success)
    {
        var record = new AuditLogRecord
        {
            UserId = command.UserId.ToString(),
            Timestamp = DateTime.UtcNow,
            Action = commandName,
            Resource = command.AuditResourceId,
            CorrelationId = correlationId,
            Success = success
        };

        // Enrich with admin-specific fields for Super Admin operations
        if (command is IAdminCommand adminCommand)
        {
            record = record with
            {
                AdminId = adminCommand.AdminId.ToString(),
                AdminTarget = adminCommand.AuditTarget,
                AdminReason = adminCommand.AuditReason
            };
        }

        return record;
    }

    private static bool IsSuccessResult(TResponse? response)
    {
        return response switch
        {
            Result result => result.IsSuccess,
            _ when IsGenericResult(response) => GetGenericResultSuccess(response),
            _ => true // Non-Result responses are assumed successful if no exception
        };
    }

    private static bool IsGenericResult(TResponse? response)
    {
        if (response is null) return false;
        var type = response.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>);
    }

    private static bool GetGenericResultSuccess(TResponse? response)
    {
        if (response is null) return true;
        var isSuccessProp = response.GetType().GetProperty("IsSuccess");
        return isSuccessProp?.GetValue(response) is true;
    }
}
