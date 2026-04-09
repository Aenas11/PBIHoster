using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

internal static class RelationalJsonConverters
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ValueConverter<List<string>, string> StringListConverter { get; } =
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeOrDefault<List<string>>(value) ?? new List<string>());

    public static ValueComparer<List<string>> StringListComparer { get; } =
        new(
            (left, right) => JsonSerializer.Serialize(left ?? new List<string>(), JsonOptions) == JsonSerializer.Serialize(right ?? new List<string>(), JsonOptions),
            value => JsonSerializer.Serialize(value ?? new List<string>(), JsonOptions).GetHashCode(StringComparison.Ordinal),
            value => value == null ? new List<string>() : value.ToList());

    public static ValueConverter<List<int>, string> IntListConverter { get; } =
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeOrDefault<List<int>>(value) ?? new List<int>());

    public static ValueComparer<List<int>> IntListComparer { get; } =
        new(
            (left, right) => JsonSerializer.Serialize(left ?? new List<int>(), JsonOptions) == JsonSerializer.Serialize(right ?? new List<int>(), JsonOptions),
            value => JsonSerializer.Serialize(value ?? new List<int>(), JsonOptions).GetHashCode(StringComparison.Ordinal),
            value => value == null ? new List<int>() : value.ToList());

    public static ValueConverter<Dictionary<string, string>, string> StringDictionaryConverter { get; } =
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeOrDefault<Dictionary<string, string>>(value) ?? new Dictionary<string, string>());

    public static ValueComparer<Dictionary<string, string>> StringDictionaryComparer { get; } =
        new(
            (left, right) => JsonSerializer.Serialize(left ?? new Dictionary<string, string>(), JsonOptions) == JsonSerializer.Serialize(right ?? new Dictionary<string, string>(), JsonOptions),
            value => JsonSerializer.Serialize(value ?? new Dictionary<string, string>(), JsonOptions).GetHashCode(StringComparison.Ordinal),
            value => value == null ? new Dictionary<string, string>() : value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

    public static ValueConverter<List<RefreshNotificationTarget>, string> NotificationTargetsConverter { get; } =
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeOrDefault<List<RefreshNotificationTarget>>(value) ?? new List<RefreshNotificationTarget>());

    public static ValueComparer<List<RefreshNotificationTarget>> NotificationTargetsComparer { get; } =
        new(
            (left, right) => JsonSerializer.Serialize(left ?? new List<RefreshNotificationTarget>(), JsonOptions) == JsonSerializer.Serialize(right ?? new List<RefreshNotificationTarget>(), JsonOptions),
            value => JsonSerializer.Serialize(value ?? new List<RefreshNotificationTarget>(), JsonOptions).GetHashCode(StringComparison.Ordinal),
            value => value == null ? new List<RefreshNotificationTarget>() : value.Select(item => new RefreshNotificationTarget { Type = item.Type, Target = item.Target }).ToList());

    private static T? DeserializeOrDefault<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}