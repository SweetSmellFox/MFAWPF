using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Utils;
using MFAWPF.Views;

namespace MFAWPF.ViewModels;

public class DragItemViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DragItemViewModel"/> class.
    /// </summary>
    /// <param name="name">The name (viewed name).</param>
    /// <param name="originalName">The original name (may not be the same as viewed name).</param>
    /// <param name="storageKey">The storage key.</param>
    public DragItemViewModel(TaskInterfaceItem interfaceItem)
    {
        InterfaceItem = interfaceItem;
        Name = interfaceItem.name;
    }


    private string _name;

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
    public bool? IsCheckedWithNull
    {
        get => _isCheckedWithNull;
        set
        {
            SetProperty(ref _isCheckedWithNull, value);
            value ??= false;
            if (InterfaceItem != null)
                InterfaceItem.check = IsChecked;
            JSONHelper.WriteToJsonFilePath(MaaProcessor.Resource, "interface", MaaInterface.Instance);
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
    public bool EnableSetting
    {
        get => _enableSetting;
        set
        {
            SetProperty(ref _enableSetting, value);
            MainWindow.Instance.SetOption(this, value);
        }
    }

    private TaskInterfaceItem _interfaceItem;

    public TaskInterfaceItem InterfaceItem
    {
        get => _interfaceItem;
        set
        {
            if (_interfaceItem != null)
            {
                Name = _interfaceItem.name;
            }

            SetProperty(ref _interfaceItem, value);
        }
    }
}