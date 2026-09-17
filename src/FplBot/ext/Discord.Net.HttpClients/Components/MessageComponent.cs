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
    [property: JsonPropertyName("label")] string? Label = null,
    [property: JsonPropertyName("custom_id")] string? CustomId = null,
    [property: JsonPropertyName("url")] string? Url = null,
    [property: JsonPropertyName("disabled")] bool? Disabled = null) : MessageComponent;

public record Section(
    [property: JsonPropertyName("components")] IReadOnlyList<MessageComponent> Components,
    [property: JsonPropertyName("accessory")] MessageComponent? Accessory = null) : MessageComponent;

public record TextDisplay(
    [property: JsonPropertyName("content")] string Content) : MessageComponent;

public record Thumbnail(
    [property: JsonPropertyName("media")] UnfurledMedia Media,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("spoiler")] bool? Spoiler = null) : MessageComponent;

public record MediaGallery(
    [property: JsonPropertyName("items")] IReadOnlyList<MediaGalleryItem> Items) : MessageComponent;

public record Separator(
    [property: JsonPropertyName("divider")] bool? Divider = null,
    [property: JsonPropertyName("spacing")] int? Spacing = null) : MessageComponent;

public record Container(
    [property: JsonPropertyName("components")] IReadOnlyList<MessageComponent> Components,
    [property: JsonPropertyName("accent_color")] int? AccentColor = null,
    [property: JsonPropertyName("spoiler")] bool? Spoiler = null) : MessageComponent;

public record MediaGalleryItem(
    [property: JsonPropertyName("media")] UnfurledMedia Media,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("spoiler")] bool? Spoiler = null);

public record UnfurledMedia(
    [property: JsonPropertyName("url")] string Url);
