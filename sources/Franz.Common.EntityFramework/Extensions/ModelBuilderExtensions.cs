using Franz.Common.Business.Domain;
using Franz.Common.Business.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Franz.Common.EntityFramework.Extensions;
public static class ModelBuilderExtensions
{
  public static ModelBuilder ConvertEnumeration(this ModelBuilder modelBuilder)
  {
    foreach (var entity in modelBuilder.Model.GetEntityTypes())
    {
      // 1. Guard against Shadow Entities (where ClrType is null)
      if (entity.ClrType == null || !entity.ClrType.IsAssignableTo(typeof(Entity)))
        continue;

      foreach (var property in entity.ClrType.GetProperties())
      {
        if (property.PropertyType.IsEnumerationClass(out var genericTypeBase))
        {
          // 2. Guard against genericTypeBase being null despite IsEnumerationClass returning true
          if (genericTypeBase == null) continue;

          var keyType = genericTypeBase.GetGenericArguments().FirstOrDefault();
          if (keyType == null) continue;

          // 3. Construct the Converter type
          var converterType = typeof(EnumerationConverter<,>)
              .MakeGenericType(property.PropertyType, keyType);

          // 4. Safely get the constructor
          var constructor = converterType.GetConstructor(Type.EmptyTypes);

          // If constructor is null here, calling .Invoke() would throw the NRE
          if (constructor == null) continue;

          var valueConverter = (ValueConverter)constructor.Invoke(null);

          modelBuilder
              .Entity(entity.ClrType)
              .Property(property.Name)
              .HasConversion(valueConverter)
              .HasColumnName($"{property.Name}Id");
        }
      }
    }

    return modelBuilder;
  }
}
