using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MFAWPF.Helper;

namespace MFAWPF.Controls;

public partial class CustomListControl
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<CustomValue<string>>),
            typeof(CustomListControl),
            new FrameworkPropertyMetadata(new ObservableCollection<CustomValue<string>>(),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnItemsSourceChanged,
                CoerceItems
            ));

    private static object CoerceItems(DependencyObject d, object value)
    {
        return value;
    }

    [Bindable(true), Category("Content"),
     DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ObservableCollection<CustomValue<string>> Items
    {
        get => (ObservableCollection<CustomValue<string>>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CustomListControl)d;

        // 解除旧集合的事件订阅
        if (e.OldValue is ObservableCollection<CustomValue<string>> oldCollection)
        {
            foreach (var item in oldCollection)
            {
                item.PropertyChanged -= control.Item_PropertyChanged;
            }

            oldCollection.CollectionChanged -= control.OnCollectionChanged;
        }

        // 订阅新集合的事件
        if (e.NewValue is ObservableCollection<CustomValue<string>> newCollection)
        {
            foreach (var item in newCollection)
            {
                item.PropertyChanged += control.Item_PropertyChanged;
            }

            newCollection.CollectionChanged += control.OnCollectionChanged;
        }

        control.OnItemsSourceChanged((IEnumerable<CustomValue<string>>)e.NewValue);
    }

    private void OnItemsSourceChanged(IEnumerable<CustomValue<string>> _)
    {
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // 处理集合的增删改
        if (e is { Action: NotifyCollectionChangedAction.Add, NewItems: not null })
        {
            foreach (CustomValue<string> item in e.NewItems)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }
        else if (e is { Action: NotifyCollectionChangedAction.Remove, OldItems: not null })
        {
            foreach (CustomValue<string> item in e.OldItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
        }
        else if (e is { Action: NotifyCollectionChangedAction.Reset })
        {
            foreach (CustomValue<string> item in Items)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        // 更新绑定的属性值
        SetCurrentValue(ItemsProperty, Items);
    }

    private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CustomValue<string>.Value))
        {
            // 手动触发 CollectionChanged 事件，通知 UI 更新
            OnCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public CustomListControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    // 删除按钮点击事件
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            var listBoxItem = FindParent<ListBoxItem>(button);
            if (listBoxItem == null) return;

            if (listBoxItem.DataContext is CustomValue<string> item)
            {
                Items.Remove(item);
            }
        }
    }

    // 添加按钮点击事件
    private void Add(object sender, RoutedEventArgs e)
    {
        Items.Add(new CustomValue<string>(""));
    }

    // 查找父元素的方法
    private static T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        for (var parent = VisualTreeHelper.GetParent(child);
             parent != null;
             parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is T foundParent)
                return foundParent;
        }

        return null;
    }


    private void Clear(object sender, RoutedEventArgs e)
    {
        Items.Clear();
    }
}