using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MFAWPF.Helper;
using Microsoft.Win32;

namespace MFAWPF.Views;

public partial class SwipeDialog
{
    private Point _startPoint;
    private Point _endPoint;
    private Line _arrowLine;
    private Polygon _arrowHead;
    private Point StartPoint { get; set; }
    private Point EndPoint { get; set; }
    private List<int> _outputBegin;
    private List<int> _outputEnd;

    public List<int> OutputBegin
    {
        get => _outputBegin;
        private set => _outputBegin = value?.Select(i => i < 0 ? 0 : i).ToList();
    }

    public List<int> OutputEnd
    {
        get => _outputEnd;
        private set => _outputEnd = value?.Select(i => i < 0 ? 0 : i).ToList();
    }

    public SwipeDialog()
    {
        InitializeComponent();
        Task.Run(() =>
        {
            var image = MaaProcessor.Instance.GetBitmapImage();
            DispatcherHelper.RunOnMainThread(() => { UpdateImage(image); });
        });
    }

    public void UpdateImage(BitmapImage imageSource)
    {
        if (imageSource == null)
            return;
        LoadingCircle.Visibility = Visibility.Collapsed;
        ImageArea.Visibility = Visibility.Visible;
        image.Source = imageSource;

        _originWidth = imageSource.PixelWidth;
        _originHeight = imageSource.PixelHeight;

        double maxWidth = image.MaxWidth;
        double maxHeight = image.MaxHeight;

        double widthRatio = maxWidth / _originWidth;
        double heightRatio = maxHeight / _originHeight;
        _scaleRatio = Math.Min(widthRatio, heightRatio);

        image.Width = _originWidth * _scaleRatio;
        image.Height = _originHeight * _scaleRatio;

        SelectionCanvas.Width = image.Width;
        SelectionCanvas.Height = image.Height;
        Width = image.Width + 20;
        Height = image.Height + 100;
        CenterWindow();
    }

    private double _scaleRatio;
    private double _originWidth;
    private double _originHeight;

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_arrowLine != null)
        {
            SelectionCanvas.Children.Remove(_arrowLine);
            SelectionCanvas.Children.Remove(_arrowHead);
        }

        // 获取鼠标点击位置
        var canvasPosition = e.GetPosition(SelectionCanvas);
        var imagePosition = e.GetPosition(image);

        // 判断点击是否在Image边缘5个像素内
        if (canvasPosition.X < image.ActualWidth + 5 && canvasPosition.Y < image.ActualHeight + 5 &&
            canvasPosition is { X: > -5, Y: > -5 })
        {
            // 如果超出5个像素内，调整点击位置到Image边界
            imagePosition.X = Math.Max(0, Math.Min(imagePosition.X, image.ActualWidth));
            imagePosition.Y = Math.Max(0, Math.Min(imagePosition.Y, image.ActualHeight));

            _startPoint = new Point(imagePosition.X + image.Margin.Left, imagePosition.Y + image.Margin.Top);

            _arrowLine = new Line
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };

            _arrowHead = new Polygon
            {
                Fill = Brushes.Red,
                Points = new PointCollection()
            };

            SelectionCanvas.Children.Add(_arrowLine);
            SelectionCanvas.Children.Add(_arrowHead);

            // 捕获鼠标事件
            Mouse.Capture(SelectionCanvas);
        }
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(image);
        position.X = Math.Max(0, Math.Min(position.X, image.ActualWidth));
        position.Y = Math.Max(0, Math.Min(position.Y, image.ActualHeight));
        MousePositionText.Text = $"[ {(int)(position.X / _scaleRatio)}, {(int)(position.Y / _scaleRatio)} ]";

        if (_arrowLine == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        _endPoint = e.GetPosition(SelectionCanvas);

        // 确保箭头的终点在Image内部
        _endPoint.X = Math.Max(0, Math.Min(_endPoint.X, image.ActualWidth));
        _endPoint.Y = Math.Max(0, Math.Min(_endPoint.Y, image.ActualHeight));

        _arrowLine.X1 = _startPoint.X;
        _arrowLine.Y1 = _startPoint.Y;
        _arrowLine.X2 = _endPoint.X;
        _arrowLine.Y2 = _endPoint.Y;

        DrawArrowHead(_startPoint, _endPoint);
        MousePositionText.Text =
            $"[ {(int)(_startPoint.X / _scaleRatio)}, {(int)(_startPoint.Y / _scaleRatio)} ] -> [ {(int)(_endPoint.X / _scaleRatio)}, {(int)(_endPoint.Y / _scaleRatio)} ]";
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_arrowLine == null)
            return;

        StartPoint = _startPoint;
        EndPoint = _endPoint;
        Mouse.Capture(null);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_arrowLine == null)
        {
            GrowlHelper.WarningGlobal("请选择一个区域");
            return;
        }

        // 输出起点和终点的坐标
        OutputBegin = [(int)(StartPoint.X / _scaleRatio), (int)(StartPoint.Y / _scaleRatio), 1, 1];
        OutputEnd = [(int)(EndPoint.X / _scaleRatio), (int)(EndPoint.Y / _scaleRatio), 1, 1];

        DialogResult = true;
        Close();
    }

    private void DrawArrowHead(Point start, Point end)
    {
        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        var arrowLength = 10;
        var arrowWidth = 5;

        var sin = Math.Sin(angle);
        var cos = Math.Cos(angle);

        var p1 = new Point(end.X - arrowLength * cos + arrowWidth * sin, end.Y - arrowLength * sin - arrowWidth * cos);
        var p2 = new Point(end.X - arrowLength * cos - arrowWidth * sin, end.Y - arrowLength * sin + arrowWidth * cos);

        _arrowHead?.Points.Clear();
        _arrowHead?.Points.Add(end);
        _arrowHead?.Points.Add(p1);
        _arrowHead?.Points.Add(p2);
    }

    private void Load(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "LoadImageTitle".ToLocalization(), Filter = "ImageFilter".ToLocalization()
        };
        if (openFileDialog.ShowDialog().IsTrue())
        {
            try
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));
                UpdateImage(bitmapImage);
            }
            catch (Exception ex)
            {
                ErrorView.ShowException(ex);
            }
        }
    }
}