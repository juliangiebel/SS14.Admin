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
        throw new NotImplementedException();
    }
}

