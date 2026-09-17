using System.Text.Json.Serialization;

namespace Discord.Net.HttpClients.Components;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ActionRow), 1)]
[JsonDerivedType(typeof(Button), 2)]
[JsonDerivedType(typeof(Section), 9)]
[JsonDerivedType(typeof(TextDisplay), 10)]
[JsonDerivedType(typeof(Thumbnail), 11)]
[JsonDerivedType(typeof(MediaGallery), 12)]
[JsonDerivedType(typeof(Separator), 14)]
[JsonDerivedType(typeof(Container), 17)]
public abstract record MessageComponent;

public record ActionRow(
    [property: JsonPropertyName("components")] IReadOnlyList<MessageComponent> Components) : MessageComponent;

public record Button(
    [property: JsonPropertyName("style")] int Style,
    [property: JsonPropertyName("label"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Label = null,
    [property: JsonPropertyName("custom_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CustomId = null,
    [property: JsonPropertyName("url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Url = null,
    [property: JsonPropertyName("disabled"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Disabled = null) : MessageComponent;

public record Section(
    [property: JsonPropertyName("components")] IReadOnlyList<MessageComponent> Components,
    [property: JsonPropertyName("accessory"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MessageComponent? Accessory = null) : MessageComponent;

public record TextDisplay(
    [property: JsonPropertyName("content")] string Content) : MessageComponent;

public record Thumbnail(
    [property: JsonPropertyName("media")] UnfurledMedia Media,
    [property: JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description = null,
    [property: JsonPropertyName("spoiler"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Spoiler = null) : MessageComponent;

public record MediaGallery(
    [property: JsonPropertyName("items")] IReadOnlyList<MediaGalleryItem> Items) : MessageComponent;

public record Separator(
    [property: JsonPropertyName("divider"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Divider = null,
    [property: JsonPropertyName("spacing"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Spacing = null) : MessageComponent;

public record Container(
    [property: JsonPropertyName("components")] IReadOnlyList<MessageComponent> Components,
    [property: JsonPropertyName("accent_color"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? AccentColor = null,
    [property: JsonPropertyName("spoiler"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Spoiler = null) : MessageComponent;

public record MediaGalleryItem(
    [property: JsonPropertyName("media")] UnfurledMedia Media,
    [property: JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description = null,
    [property: JsonPropertyName("spoiler"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Spoiler = null);

public record UnfurledMedia(
    [property: JsonPropertyName("url")] string Url);
