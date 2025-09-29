using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.MongoDB.Configuration;
public class MongoDBConfig
{
  public string? ConnectionString { get; set; }
  public string? Database { get; set; }
  public string? Collection { get; set; }
}
