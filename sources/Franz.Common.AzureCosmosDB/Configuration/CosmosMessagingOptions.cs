using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Franz.Common.AzureCosmosDB.Configuration;

public sealed class CosmosMessagingOptions
{
  [Required] 
  public string ContainerName { get; set; } = "Messages";
  
  [Range(1, 365)] 
  public int RetentionDays { get; set; } = 7;
}