using CommunityToolkit.Mvvm.ComponentModel;

namespace MFAWPF.Helper;

public class CustomValue<T> : ObservableObject
{
    public CustomValue(T value)
    {
        Value = value;
    }
    public CustomValue(string name,T value)
    {
        Name = name;
        Value = value;
    }

    private T _value;

    public T Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    private string _name;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public override string ToString()
    {
        return Value?.ToString() ?? string.Empty;
    }
}
