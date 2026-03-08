using Microsoft.Windows.Storage;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WinUIGallery.Helpers;

public partial class ApplicationDataSettingsProvider : ISettingsProvider
{
    private readonly ApplicationDataContainer container;

    public ApplicationDataSettingsProvider(ApplicationDataContainer container)
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public bool Contains(string key) => container.Values.ContainsKey(key);

    public object Get(string key) => container.Values.TryGetValue(key, out object? value) ? value! : null!;

    public void Set(string key, object value) => container.Values[key] = value;

    public T Get<T>(string key)
    {
        if (!container.Values.TryGetValue(key, out var value))
            return default!;

        if (value is T t)
            return t;

        if (value is string str && !IsSimpleType(typeof(T)))
        {
            try
            {
                var typeInfo = SettingsJsonContext.Default.GetTypeInfo(typeof(T));
                if (typeInfo is null)
                    return default!;

                object? deserialized = JsonSerializer.Deserialize(str, typeInfo);
                return deserialized is T typed ? typed : default!;
            }
            catch (Exception)
            {
                HandleCorruptedKey(key);
                return default!;
            }
        }

        object? converted = Convert.ChangeType(value, typeof(T));
        return converted is T typedConverted ? typedConverted : default!;
    }

    private void HandleCorruptedKey(string key)
    {
        try
        {
            container.Values.Remove(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to remove corrupted key '{key}': {ex}");
        }
    }

    public void Set<T>(string key, T value)
    {
        object storedValue;
        if (IsSimpleType(typeof(T)))
        {
            storedValue = value!;
        }
        else
        {
            var typeInfo = SettingsJsonContext.Default.GetTypeInfo(typeof(T));
            storedValue = typeInfo is null ? string.Empty : JsonSerializer.Serialize(value, typeInfo);
        }

        container.Values[key] = storedValue;
    }

    private static readonly HashSet<Type> ExtraSimpleTypes = new()
    {
        typeof(string),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Windows.Foundation.Point),
        typeof(Windows.Foundation.Size),
        typeof(Windows.Foundation.Rect)
    };

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || ExtraSimpleTypes.Contains(type);
    }
}
