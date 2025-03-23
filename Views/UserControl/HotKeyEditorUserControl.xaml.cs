    using MFAWPF.Helper;
    using MFAWPF.Helper.ValueType;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;

    namespace MFAWPF.Views.UserControl;

    public partial class HotKeyEditorUserControl
    {
        public static readonly DependencyProperty HotKeyProperty =
            DependencyProperty.Register(nameof(HotKey), typeof(MFAHotKey),
                typeof(HotKeyEditorUserControl),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public MFAHotKey HotKey
        {
            get => (MFAHotKey)GetValue(HotKeyProperty);
            set => SetValue(HotKeyProperty, value);
        }

        public HotKeyEditorUserControl()
        {
            InitializeComponent();
        }

        private void HotKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // Remove hotKey if no modifiers
            if (modifiers == ModifierKeys.None)
            {
                HotKey = MFAHotKey.NOTSET;
                return;
            }

            // If no actual key was pressed - return
            if (key == Key.LeftCtrl
                || key == Key.RightCtrl
                || key == Key.LeftAlt
                || key == Key.RightAlt
                || key == Key.LeftShift
                || key == Key.RightShift
                || key == Key.LWin
                || key == Key.RWin
                || key == Key.Clear
                || key == Key.OemClear
                || key == Key.Apps)
            {
                return;
            }
            
            HotKey = new MFAHotKey(key, modifiers);
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => 
            {
                Keyboard.ClearFocus();
                
                var parent = Keyboard.FocusedElement as FrameworkElement;
                parent?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
            }));
        }

        private void HotKey_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;
            HotKeyHelper.UnregisterHotKey(Application.Current.MainWindow, HotKey); 
            HotKey = MFAHotKey.PRESSING;
        }
    }
