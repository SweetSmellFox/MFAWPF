using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace MFAWPF.Controls;

public class CustomWindow : Window
{
    Point _pressedPosition;
    bool _isDragMoved;


    public static readonly DependencyProperty IsResizableProperty =
        DependencyProperty.Register(nameof(IsResizable), typeof(bool), typeof(CustomWindow),
            new PropertyMetadata(false));

    public static readonly DependencyProperty DragHandleNameProperty =
        DependencyProperty.Register(nameof(DragHandleName), typeof(string), typeof(CustomWindow),
            new PropertyMetadata("TitleBar"));

    public string DragHandleName
    {
        get => (string)GetValue(DragHandleNameProperty);
        set => SetValue(DragHandleNameProperty, value);
    }

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
        hwndSource?.AddHook(WndProc);
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

    protected virtual void Close(object sender = null, RoutedEventArgs e = null)
    {
        base.Close();
    }

    protected void btnClose_Click(object sender, RoutedEventArgs e)
    {
        base.Close();
    }

    protected void btnRestore_Click(object sender, RoutedEventArgs e)
    {
        var titleBar = FindName("TitleBar") as FrameworkElement;
        if (WindowState == WindowState.Normal)
        {
            var workingArea = SystemParameters.WorkArea;

            MaxHeight = workingArea.Height + 8;

            WindowState = WindowState.Maximized;

            if (titleBar != null) titleBar.Margin = new Thickness(6, 5, 6, 0);
        }
        else
        {
            if (titleBar != null)
                titleBar.Margin = new Thickness(0);
            WindowState = WindowState.Normal;
        }
    }

    protected void btnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    protected void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pressedPosition = e.GetPosition(this);
    }

    protected void Window_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (Mouse.LeftButton == MouseButtonState.Pressed && _pressedPosition != e.GetPosition(this))
        {
            if (FindName(DragHandleName) is UIElement element && IsPointInElement(e.GetPosition(this), element))
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

    private bool IsPointInElement(Point point, UIElement element)
    {
        if (element == null) return false;

        var transform = element.TransformToVisual(this);
        var elementPoint = transform.Transform(new Point(0, 0));
        var elementRect = new Rect(elementPoint, new Size(element.RenderSize.Width, element.RenderSize.Height));

        return elementRect.Contains(point);
    }
    
    public void CenterWindow()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        
        var left = (screenWidth - Width) / 2;
        var top = (screenHeight - Height) / 2;
        
        this.Left = left;
        this.Top = top;
    }
}