namespace Franz.Common.Mediator.Options
{
  public class TransactionOptions
  {
    /// <summary>
    /// If true, rollback on all exceptions (default).
    /// If false, rollback only for configured exceptions.
    /// </summary>
    public bool RollbackOnAnyException { get; set; } = true;

    /// <summary>
    /// List of exception types that should trigger rollback when RollbackOnAnyException is false.
    /// </summary>
    public HashSet<Type> RollbackOnExceptions { get; } = new();

    /// <summary>
    /// Optional predicate to allow advanced conditions (e.g., based on exception content).
    /// </summary>
    public Func<Exception, bool>? RollbackCondition { get; set; }
  }
}
