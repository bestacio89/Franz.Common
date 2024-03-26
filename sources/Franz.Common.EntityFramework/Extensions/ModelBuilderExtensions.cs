using Franz.Common.Business.Domain;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework.Extensions;
public static class ModelBuilderExtensions
{
  public static ModelBuilder ConvertEnumeration(this ModelBuilder modelBuilder)
  {
    foreach (var entity in modelBuilder.Model.GetEntityTypes())
    {
      if (entity.ClrType.IsAssignableTo(typeof(Entity)))
      {
        foreach (var property in entity.ClrType.GetProperties().AsEnumerable())
        {
          if (property.PropertyType.IsEnumerationClass(out var genericTypeBase))
          {
            var keyType = genericTypeBase!.GetGenericArguments().First();
            var enumerationConverterTypeBase = typeof(EnumerationConverter<,>).MakeGenericType(property.PropertyType, keyType);
            var constructor = enumerationConverterTypeBase.GetConstructor(Array.Empty<Type>());
            var valueConverter = (Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter)constructor!.Invoke(null);

            modelBuilder
                .Entity(entity.ClrType)
                .Property(property.Name)
                .HasConversion(valueConverter)
                .HasColumnName($"{property.Name}Id");
          }
        }
      }
    }

    return modelBuilder;
  }
}
