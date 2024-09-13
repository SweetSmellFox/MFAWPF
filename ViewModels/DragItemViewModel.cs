using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Data;
using MFAWPF.Utils;
using MFAWPF.Views;
using Newtonsoft.Json;

namespace MFAWPF.ViewModels;

public class DragItemViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DragItemViewModel"/> class.
    /// </summary>
    /// <param name="name">The name (viewed name).</param>
    /// <param name="originalName">The original name (may not be the same as viewed name).</param>
    /// <param name="storageKey">The storage key.</param>
    public DragItemViewModel(TaskInterfaceItem? interfaceItem)
    {
        InterfaceItem = interfaceItem;
        Name = interfaceItem?.name ?? "未命名";
    }


    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private bool? _isCheckedWithNull = false;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the key is checked with null.
    /// </summary>
    [JsonIgnore]
    public bool? IsCheckedWithNull
    {
        get => _isCheckedWithNull;
        set
        {
            SetProperty(ref _isCheckedWithNull, value);
            value ??= false;
            if (InterfaceItem != null)
                InterfaceItem.check = IsChecked;
            JsonHelper.WriteToJsonFilePath(AppDomain.CurrentDomain.BaseDirectory, "interface", MaaInterface.Instance);
            DataSet.SetData("Tasks", MainWindow.Data?.TaskItemViewModels.ToList());
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the key is checked.
    /// </summary>
    public bool IsChecked
    {
        get => IsCheckedWithNull != false;
        set => IsCheckedWithNull = value;
    }


    private bool _enableSetting;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the setting enabled.
    /// </summary>
    [JsonIgnore]
    public bool EnableSetting
    {
        get => _enableSetting;
        set
        {
            SetProperty(ref _enableSetting, value);
            MainWindow.Instance?.SetOption(this, value);
        }
    }

    private TaskInterfaceItem? _interfaceItem;

    public TaskInterfaceItem? InterfaceItem
    {
        get => _interfaceItem;
        set
        {
            if (_interfaceItem != null)
            {
                if (_interfaceItem.name != null)
                    Name = _interfaceItem.name;
                if ((_interfaceItem.option == null || _interfaceItem.option?.Count == 0) &&
                    _interfaceItem.repeatable == false)
                    SettingVisibility = Visibility.Hidden;
            }

            SetProperty(ref _interfaceItem, value);
        }
    }

    private Visibility _visibility = Visibility.Visible;

    public Visibility SettingVisibility
    {
        get => _visibility;
        set => SetProperty(ref _visibility, value);
    }

    /// <summary>
    /// Creates a deep copy of the current <see cref="DragItemViewModel"/> instance.
    /// </summary>
    /// <returns>A new <see cref="DragItemViewModel"/> instance that is a deep copy of the current instance.</returns>
    public DragItemViewModel Clone()
    {
        // Clone the InterfaceItem if it's not null
        TaskInterfaceItem? clonedInterfaceItem = InterfaceItem?.Clone();

        // Create a new DragItemViewModel instance with the cloned InterfaceItem
        DragItemViewModel clone = new DragItemViewModel(clonedInterfaceItem);

        // Copy all other properties to the new instance
        clone.Name = this.Name;
        clone.IsCheckedWithNull = this.IsCheckedWithNull;
        clone.EnableSetting = this.EnableSetting;
        clone.SettingVisibility = this.SettingVisibility;

        return clone;
    }
}