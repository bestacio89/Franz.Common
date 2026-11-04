namespace Franz.Common.Messaging.Configuration;

/// <summary>
/// Controls whether a transport initializer creates topology, verifies existing topology,
/// or deliberately performs no provisioning.
/// </summary>
public enum ProvisioningMode
{
  Create,
  Verify,
  Off
}
