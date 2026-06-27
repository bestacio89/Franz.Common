using Franz.Common.Business.Domain;
using Franz.Common.Business.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Franz.Common.EntityFramework.Extensions;
public static class ModelBuilderExtensions
{
  private static bool ImplementsIEntity(Type type)
  {
    return type.GetInterfaces()
        .Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEntity<>));
  }
  public static ModelBuilder ConvertEnumeration(this ModelBuilder modelBuilder)
  {
    foreach (var entity in modelBuilder.Model.GetEntityTypes())
    {
      var clrType = entity.ClrType;

      // =========================================================
      // 1. Skip shadow or non-domain entities
      // =========================================================
      if (clrType == null || !ImplementsIEntity(clrType))
        continue;

      foreach (var property in clrType.GetProperties())
      {
        if (!property.PropertyType.IsEnumerationClass(out var genericTypeBase))
          continue;

        if (genericTypeBase is null)
          continue;

        var keyType = genericTypeBase.GetGenericArguments().FirstOrDefault();
        if (keyType is null)
          continue;

        // =========================================================
        // 2. Create converter safely
        // =========================================================
        var converterType = typeof(EnumerationConverter<,>)
            .MakeGenericType(property.PropertyType, keyType);

        if (Activator.CreateInstance(converterType) is not ValueConverter valueConverter)
          continue;

        // =========================================================
        // 3. Apply EF conversion
        // =========================================================
        modelBuilder
            .Entity(clrType)
            .Property(property.Name)
            .HasConversion(valueConverter)
            .HasColumnName($"{property.Name}Id");
      }
    }

    return modelBuilder;
  }
}
