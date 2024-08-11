using System.Windows;
using System.Windows.Input;
using MFAWPF.Views;

namespace MFAWPF.Controls;

public partial class CustomWindow : Window
{
    Point _pressedPosition;
    bool _isDragMoved = false;

    public CustomWindow() : base()
    {
        ResizeMode = ResizeMode.NoResize;
        PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;
        PreviewMouseMove += Window_PreviewMouseMove;
        PreviewMouseLeftButtonUp += Window_PreviewMouseLeftButtonUp;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    protected void Close(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    protected void Window_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _pressedPosition = e.GetPosition(this);
    }

    protected void Window_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (Mouse.LeftButton == MouseButtonState.Pressed && _pressedPosition != e.GetPosition(this))
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var mainWidth = this.Width;
            //判断鼠标位置  
            Point pp = Mouse.GetPosition(this);
            if (pp.X >= 81 && pp.X <= 1679 && pp.Y >= 1 && pp.Y <= 40)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _isDragMoved = true;
                    DragMove();
                }
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