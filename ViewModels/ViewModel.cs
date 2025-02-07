using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MFAWPF.ViewModels;

public class ViewModel : ObservableObject
{
    protected bool SetCurrentProperty<T>([NotNullIfNotNull("newValue")] ref T field, T newValue, [CallerMemberName] string propertyName = null)
    {
        OnPropertyChanging(propertyName);
        field = newValue;
        OnPropertyChanged(propertyName);
        return true;
    }

}
