using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace MFAWPF.Controls;

public class CustomWindow : Window
{
    Point _pressedPosition;
    bool _isDragMoved = false;

    // 定义一个依赖属性来控制四周缩放
    public static readonly DependencyProperty IsResizableProperty =
        DependencyProperty.Register(nameof(IsResizable), typeof(bool), typeof(CustomWindow),
            new PropertyMetadata(false));

    public bool IsResizable
    {
        get => (bool)GetValue(IsResizableProperty);
        set => SetValue(IsResizableProperty, value);
    }

    public CustomWindow()
    {
        ResizeMode = ResizeMode.CanResize;
        PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;
        PreviewMouseMove += Window_PreviewMouseMove;
        PreviewMouseLeftButtonUp += Window_PreviewMouseLeftButtonUp;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brushes.Transparent;
        MinWidth = 200;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = (new WindowInteropHelper(this)).Handle;
        var hwndSource = HwndSource.FromHwnd(handle);
        hwndSource.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTCLIENT = 1;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;

        if (msg == WM_NCHITTEST && IsResizable)
        {
            handled = true;
            var mousePos = PointFromScreen(new Point((int)lParam & 0xFFFF, (int)lParam >> 16));
            var result = HTCLIENT;

            double resizeBorderThickness = 8;

            if (mousePos.Y < resizeBorderThickness)
            {
                if (mousePos.X < resizeBorderThickness) result = HTTOPLEFT;
                else if (mousePos.X > ActualWidth - resizeBorderThickness) result = HTTOPRIGHT;
                else result = HTTOP;
            }
            else if (mousePos.Y > ActualHeight - resizeBorderThickness)
            {
                if (mousePos.X < resizeBorderThickness) result = HTBOTTOMLEFT;
                else if (mousePos.X > ActualWidth - resizeBorderThickness) result = HTBOTTOMRIGHT;
                else result = HTBOTTOM;
            }
            else if (mousePos.X < resizeBorderThickness)
            {
                result = HTLEFT;
            }
            else if (mousePos.X > ActualWidth - resizeBorderThickness)
            {
                result = HTRIGHT;
            }

            return new IntPtr(result);
        }

        return IntPtr.Zero;
    }

    protected virtual void Close(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pressedPosition = e.GetPosition(this);
    }

    protected void Window_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (Mouse.LeftButton == MouseButtonState.Pressed && _pressedPosition != e.GetPosition(this))
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragMoved = true;
                DragMove();
            }
        }
    }

    protected void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragMoved)
        {
            _isDragMoved = false;
            e.Handled = true;
        }
    }
}