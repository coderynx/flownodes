using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace Flownodes.Cluster.Node.Automation;

public class LoggerStep : StepBody
{
    private readonly ILogger<LoggerStep> _logger;

    public LoggerStep(ILogger<LoggerStep> logger)
    {
        _logger = logger;
    }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        _logger.LogInformation("Hello from logger step: {StepId}", context.Step.Id);
        return ExecutionResult.Next();
    }
}