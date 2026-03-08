using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinUIGallery.Helpers;

public partial class ObservableSettings : INotifyPropertyChanged
{
    private readonly ISettingsProvider provider;

    public ObservableSettings(ISettingsProvider provider)
    {
        this.provider = provider;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(T value, [CallerMemberName] string? propertyName = null)
    {
        string key = propertyName ?? string.Empty;

        if (provider.Contains(key))
        {
            T? currentValue = provider.Get<T>(key);
            if (Equals(currentValue, value))
                return false;
        }

        provider.Set(key, value!);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
        return true;
    }

    protected T? Get<T>([CallerMemberName] string? propertyName = null)
    {
        return provider.Get<T>(propertyName ?? string.Empty);
    }

    protected T GetOrCreateDefault<T>(T defaultValue, [CallerMemberName] string? propertyName = null)
    {
        string key = propertyName ?? string.Empty;
        if (!provider.Contains(key))
            Set(defaultValue, key);

        T? value = Get<T>(key);
        return value is null ? defaultValue : value;
    }
}
