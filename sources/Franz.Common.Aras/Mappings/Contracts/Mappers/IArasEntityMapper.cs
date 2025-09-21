using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Mappings.Contracts.Mappers;
/// <summary>
/// Maps between ARAS API data (JSON/XML) and strongly typed entities.
/// </summary>
public interface IArasEntityMapper<TEntity>
{
  TEntity MapFromAras(IDictionary<string, object> arasData);
  IDictionary<string, object> MapToAras(TEntity entity);
}