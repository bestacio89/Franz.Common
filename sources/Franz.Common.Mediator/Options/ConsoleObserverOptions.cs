namespace Franz.Common.Mediator.Options
{
  public class ConsoleObserverOptions
  {
    /// <summary>
    /// Show full request type name (namespace + class). If false, only class name.
    /// </summary>
    public bool ShowFullTypeName { get; set; } = false;

    /// <summary>
    /// Print responses (ToString) after completion.
    /// </summary>
    public bool ShowResponse { get; set; } = true;

    /// <summary>
    /// Print exception stack traces when failures occur.
    /// </summary>
    public bool ShowStackTrace { get; set; } = false;

    /// <summary>
    /// Print per-notification handler telemetry.
    /// </summary>
    public bool ShowNotificationHandlers { get; set; } = true;

    /// <summary>
    /// Enable colored output.
    /// </summary>
    public bool UseColors { get; set; } = true;
  }
}
