using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace SS14.Admin.Components.Shared.Filter;

public partial class Filter : ComponentBase
{
    [Parameter] public IFilterModel Model { get; set; } = null!;


    private void OnInput(ChangeEventArgs args, PropertyInfo property)
    {
        property.SetValue(Model, args.Value);
    }

    private void OnEnumInput(ChangeEventArgs args, PropertyInfo property)
    {
        if (!property.PropertyType.IsEnum)
            throw new InvalidOperationException($"Property {property.Name} is not an enum type.");

        if (args.Value == null && Nullable.GetUnderlyingType(property.PropertyType) == null)
                throw new InvalidOperationException($"Property {property.Name} is not nullable.");


        Enum.TryParse(property.PropertyType, args.Value?.ToString(), out var enumValue);
        property.SetValue(Model, enumValue);
    }
}

