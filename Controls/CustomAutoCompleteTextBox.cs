using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MFAWPF.ViewModels;
using HandyControl.Controls;
using HandyControl.Tools.Helper;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;

namespace MFAWPF.Controls;

public class CustomAutoCompleteTextBox : AutoCompleteTextBox
{
    private const string SearchTextBox = "PART_SearchTextBox";
    private bool ignoreTextChanging;
    private TextBox _searchTextBox;

    private object
        _selectedItem;

    private bool isApplyingTemplate;
    private bool isSelectionChanging;

    public static readonly DependencyProperty DataListProperty = DependencyProperty
        .Register(nameof(DataList), typeof(IEnumerable), typeof(CustomAutoCompleteTextBox),
            new FrameworkPropertyMetadata(null, OnDataListChanged));

    public IEnumerable DataList
    {
        get => GetValue(DataListProperty) as IEnumerable;
        set =>
            SetValue(DataListProperty, value);
    }

    private static void OnDataListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomAutoCompleteTextBox sAutoCompleteTextBox && e.NewValue != null)
        {
            sAutoCompleteTextBox.SetCurrentValue(ItemsSourceProperty, e.NewValue);
        }
    }

    static CustomAutoCompleteTextBox()
    {
        TextProperty.OverrideMetadata(typeof(CustomAutoCompleteTextBox),
            new FrameworkPropertyMetadata(String.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                OnTextChanged));
    }

    public TextBox GetTextBox()
    {
        return _searchTextBox;
    }

    public CustomAutoCompleteTextBox()
    {
        Style = FindResource("AutoCompleteTextBoxBaseStyle") as Style;
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomAutoCompleteTextBox sAutoCompleteTextBox && e.NewValue != null)
        {
            var textBox = sAutoCompleteTextBox.GetTextBox();
            if (textBox != null)
            {
                textBox.Text = e.NewValue.ToString() ?? string.Empty;
            }
        }
    }


    public override void OnApplyTemplate()
    {
        try
        {
            if (isApplyingTemplate)
            {
                return;
            }

            isApplyingTemplate = true;

            if (_searchTextBox != null)
            {
                _searchTextBox.GotFocus -= SearchTextBoxGotFocus;
                _searchTextBox.KeyDown -= SearchTextBoxKeyDown;
                _searchTextBox.TextChanged -= SearchTextBoxTextChanged;
                _searchTextBox.Text = Text;
            }

            // 调用 ComboBox 的 OnApplyTemplate 方法
            MethodInfo baseMethod =
                typeof(ComboBox).GetMethod("OnApplyTemplate", BindingFlags.Instance | BindingFlags.Public);
            if (baseMethod != null && baseMethod.DeclaringType == typeof(ComboBox))
            {
                baseMethod.Invoke(this, null);
            }

            _searchTextBox = GetTemplateChild(SearchTextBox) as TextBox;
            if (_searchTextBox != null)
            {
                _searchTextBox.GotFocus += SearchTextBoxGotFocus;
                _searchTextBox.PreviewKeyDown += SearchTextBoxKeyDown;
                _searchTextBox.TextChanged += SearchTextBoxTextChanged;
                _searchTextBox.Text = Text;
            }

            UpdateTextBoxBySelectedItem(_selectedItem);
            isApplyingTemplate = false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        try
        {
            if (isSelectionChanging)
            {
                return;
            }

            isSelectionChanging = true;

            // 调用 ComboBox 的 OnSelectionChanged 方法
            MethodInfo baseMethod =
                typeof(ComboBox).GetMethod("OnSelectionChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            if (baseMethod != null && baseMethod.DeclaringType == typeof(ComboBox))
            {
                baseMethod.Invoke(this, [e]);
            }

            if (e.AddedItems is { Count: > 0 } && e.AddedItems[0] is not null)
            {
                _selectedItem = e.AddedItems[0];
                UpdateTextBoxBySelectedItem(_selectedItem);
            }

            isSelectionChanging = false;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is AutoCompleteTextBoxItem;

    protected override DependencyObject GetContainerForItemOverride() => new AutoCompleteTextBoxItem();

    private void SearchTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            _selectedItem = null;
            SelectedIndex = -1;

            if (ignoreTextChanging)
            {
                ignoreTextChanging = false;
                return;
            }

            Text = _searchTextBox?.Text ?? string.Empty;

            if (DataList is IEnumerable<TaskItemViewModel> ts)
            {
                ItemsSource = ts.Where(t => t.Name.ToLower().Contains(Text.ToLower()));
            }

            if (_searchTextBox?.IsFocused == true)
            {
                SetCurrentValue(IsDropDownOpenProperty, true);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    private void SearchTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key == Key.Up)
            {
                var index = SelectedIndex - 1;
                if (index < 0)
                {
                    index = Items.Count - 1;
                }

                UpdateTextBoxBySelectedIndex(index);
            }
            else if (e.Key == Key.Down)
            {
                var index = SelectedIndex + 1;
                if (index >= Items.Count)
                {
                    index = 0;
                }

                UpdateTextBoxBySelectedIndex(index);
            }
            else if (e.Key == Key.Enter)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
                e.Handled = true;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    private void UpdateTextBoxBySelectedIndex(int selectedIndex)
    {
        try
        {
            if (_searchTextBox == null)
            {
                return;
            }

            ignoreTextChanging = true;

            if (ItemContainerGenerator.ContainerFromIndex(selectedIndex) is AutoCompleteTextBoxItem boxItem)
            {
                _searchTextBox.Text = BindingHelper.GetString(boxItem.Content, DisplayMemberPath);
                _searchTextBox.CaretIndex = _searchTextBox.Text.Length;

                SelectedIndex = selectedIndex;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void UpdateTextBoxBySelectedItem(object selectedItem)
    {
        try
        {
            if (_searchTextBox == null)
            {
                return;
            }

            ignoreTextChanging = true;

            string toString = BindingHelper.GetString(selectedItem, DisplayMemberPath);
            if (!string.IsNullOrEmpty(toString))
            {
                _searchTextBox.Text = toString;
                _searchTextBox.CaretIndex = _searchTextBox.Text.Length;

                ignoreTextChanging = true;

                Text = _searchTextBox.Text;
            }

            ignoreTextChanging = false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void SearchTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Text) || Text.Equals(""))
            SetCurrentValue(IsDropDownOpenProperty, true);
    }
}