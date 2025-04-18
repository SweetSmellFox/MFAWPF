﻿using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Configuration;
using MFAWPF.Extensions;
using MFAWPF.Helper;
using MFAWPF.Helper.ValueType;
using Newtonsoft.Json;
using System.Windows;

namespace MFAWPF.ViewModels.Tool;

public partial class DragItemViewModel : ViewModel
{
    public DragItemViewModel(TaskInterfaceItem? interfaceItem)
    {
        InterfaceItem = interfaceItem;
        Name = interfaceItem?.Name ?? "未命名";
        LanguageHelper.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty] private string _name = string.Empty;


    private bool? _isCheckedWithNull = false;
    private bool _isInitialized;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the key is checked with null.
    /// </summary>
    [JsonIgnore]
    public bool? IsCheckedWithNull
    {
        get => _isCheckedWithNull;
        set
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                SetProperty(ref _isCheckedWithNull, value);
                if (InterfaceItem != null)
                    InterfaceItem.Check = IsChecked;
            }
            else
            {
                SetProperty(ref _isCheckedWithNull, value);
                if (InterfaceItem != null)
                    InterfaceItem.Check = _isCheckedWithNull;
                ConfigurationHelper.SetValue(ConfigurationKeys.TaskItems,
                    Instances.TaskQueueViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            }
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
            Instances.TaskOptionSettingsUserControl.SetOption(this, value);
        }
    }

    private TaskInterfaceItem? _interfaceItem;

    public TaskInterfaceItem? InterfaceItem
    {
        get => _interfaceItem;
        set
        {
            if (value != null)
            {
                if (value.Name != null)
                    Name = value.Name;
                SettingVisibility = value is { Advanced.Count: > 0 } || value is { Option.Count: > 0 } || value.Repeatable.IsTrue() || value.Document != null && value.Document.Count > 0
                    ? Visibility.Visible
                    : Visibility.Hidden;
                IsCheckedWithNull = value.Check;
            }

            SetProperty(ref _interfaceItem, value);
        }
    }

    [ObservableProperty] private Visibility _settingVisibility = Visibility.Visible;


    private void UpdateContent()
    {
        if (!string.IsNullOrEmpty(InterfaceItem?.Name))
        {
            Name = LanguageHelper.GetLocalizedString(Name);
        }
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        UpdateContent();
    }

    /// <summary>
    /// Creates a deep copy of the current <see cref="DragItemViewModel"/> instance.
    /// </summary>
    /// <returns>A new <see cref="DragItemViewModel"/> instance that is a deep copy of the current instance.</returns>
    public DragItemViewModel Clone()
    {
        // Clone the InterfaceItem if it's not null
        TaskInterfaceItem clonedInterfaceItem = InterfaceItem?.Clone();

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
