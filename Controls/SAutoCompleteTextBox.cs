using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MFAWPF.ViewModels;
using HandyControl.Controls;
using HandyControl.Tools.Helper;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;

namespace MFAWPF.Controls
{
    public class SAutoCompleteTextBox : AutoCompleteTextBox
    {
        private const string SearchTextBox = "PART_SearchTextBox";
        private bool ignoreTextChanging;
        private System.Windows.Controls.TextBox _searchTextBox;
        private object _selectedItem;
        private bool isApplyingTemplate;
        private bool isSelectionChanging;

        public static readonly DependencyProperty DataListProperty = DependencyProperty
            .Register("DataList", typeof(Collection<TaskItemViewModel>), typeof(SAutoCompleteTextBox),
                new FrameworkPropertyMetadata(null));

        public Collection<TaskItemViewModel> DataList
        {
            get => GetValue(DataListProperty) as Collection<TaskItemViewModel>;
            set
            {
                OnDataList(value);
                SetValue(DataListProperty, value);
            }
        }

        private void OnDataList(object value)
        {
            this.SetResourceReference(ItemsSourceProperty, value);
        }

        static SAutoCompleteTextBox()
        {
            TextProperty.OverrideMetadata(typeof(SAutoCompleteTextBox),
                new FrameworkPropertyMetadata(String.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                    OnTextChanged));
        }

        public System.Windows.Controls.TextBox GetTextBox()
        {
            return _searchTextBox;
        }

        public SAutoCompleteTextBox()
        {
            Style = FindResource("AutoCompleteTextBoxBaseStyle") as Style;
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (((SAutoCompleteTextBox)d).GetTextBox() == null)
            {
                return;
            }

            ((SAutoCompleteTextBox)d).GetTextBox().Text = e.NewValue.ToString();
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
                    baseMethod.Invoke(this, new object[] { e });
                }

                if (e.AddedItems.Count > 0)
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

                Text = _searchTextBox.Text;

                if (DataList != null)
                {
                    ItemsSource = DataList.Where(t => t.Name.ToLower().Contains(Text.ToLower()));
                }

                if (_searchTextBox.IsFocused)
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
}