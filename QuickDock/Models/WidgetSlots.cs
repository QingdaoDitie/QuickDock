namespace QuickDock.Models;

public enum WidgetKind
{
    Weather,
    Device,
    Music,
    Quota
}

public sealed record WidgetSlots(WidgetKind First, WidgetKind Second)
{
    public static WidgetSlots Default { get; } = new(WidgetKind.Weather, WidgetKind.Quota);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsValid => Enum.IsDefined(First) && Enum.IsDefined(Second) && First != Second;
}
