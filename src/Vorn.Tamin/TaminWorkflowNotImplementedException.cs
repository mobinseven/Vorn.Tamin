namespace Vorn.Tamin;

/// <summary>Signals that a named provider workflow is intentionally unavailable in this SDK surface.</summary>
public sealed class TaminWorkflowNotImplementedException : NotSupportedException
{
    private TaminWorkflowNotImplementedException(string workflowName, string reason)
        : base($"The '{workflowName}' workflow is not implemented. {reason}")
    {
        WorkflowName = workflowName;
    }

    /// <summary>The unavailable workflow name.</summary>
    public string WorkflowName { get; }

    internal static TaminWorkflowNotImplementedException For(string workflowName, string reason)
        => new(workflowName, reason);
}
