
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MFAWPF.Helper;
using Microsoft.Win32;

namespace MFAWPF.Views;

public partial class ColorExtractionDialog
{
    private Point _startPoint;
    private Rectangle _selectionRectangle;
    private List<int> _outputRoi;

    public List<int> OutputRoi
    {
        get => _outputRoi;
        set => _outputRoi = value?.Select(i => i < 0 ? 0 : i).ToList();
    }

    public List<int> OutputUpper { get; set; }
    public List<int> OutputLower { get; set; }

    public ColorExtractionDialog()
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

        double imageWidth = imageSource.PixelWidth;
        double imageHeight = imageSource.PixelHeight;

        double maxWidth = image.MaxWidth;
        double maxHeight = image.MaxHeight;

        double widthRatio = maxWidth / imageWidth;
        double heightRatio = maxHeight / imageHeight;
        _scaleRatio = Math.Min(widthRatio, heightRatio);

        image.Width = imageWidth * _scaleRatio;
        image.Height = imageHeight * _scaleRatio;

        SelectionCanvas.Width = image.Width;
        SelectionCanvas.Height = image.Height;
        Width = image.Width + 20;
        Height = image.Height + 100;
        CenterWindow();
    }

    private double _scaleRatio;
    

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(image);
        var canvasPosition = e.GetPosition(SelectionCanvas);

        // 判断点击是否在Image边缘5个像素内
        if (canvasPosition.X < image.ActualWidth + 5 && canvasPosition.Y < image.ActualHeight + 5 &&
            canvasPosition is { X: >= -5, Y: >= -5 })
        {
            if (_selectionRectangle != null)
            {
                SelectionCanvas.Children.Remove(_selectionRectangle);
            }

            // 如果超出5个像素内，调整点击位置到Image边界
            if (position.X < 0) position.X = 0;
            if (position.Y < 0) position.Y = 0;
            if (position.X > image.ActualWidth) position.X = image.ActualWidth;
            if (position.Y > image.ActualHeight) position.Y = image.ActualHeight;

            _startPoint = position;

            _selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2.5,
                StrokeDashArray = { 2 }
            };

            Canvas.SetLeft(_selectionRectangle, _startPoint.X);
            Canvas.SetTop(_selectionRectangle, _startPoint.Y);

            SelectionCanvas.Children.Add(_selectionRectangle);

            // 捕获鼠标事件
            Mouse.Capture(SelectionCanvas);
        }
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(image);
        MousePositionText.Text = $"[ {(int)(position.X / _scaleRatio)}, {(int)(position.Y / _scaleRatio)} ]";

        if (_selectionRectangle == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var pos = e.GetPosition(SelectionCanvas);

        var x = Math.Min(pos.X, _startPoint.X);
        var y = Math.Min(pos.Y, _startPoint.Y);

        var w = Math.Abs(pos.X - _startPoint.X);
        var h = Math.Abs(pos.Y - _startPoint.Y);

        // 确保矩形不会超出左边和上边
        if (x < 0)
        {
            x = 0;
            w = _startPoint.X;
        }

        if (y < 0)
        {
            y = 0;
            h = _startPoint.Y;
        }

        if (x + w > SelectionCanvas.ActualWidth)
        {
            w = SelectionCanvas.ActualWidth - x;
        }

        if (y + h > SelectionCanvas.ActualHeight)
        {
            h = SelectionCanvas.ActualHeight - y;
        }

        _selectionRectangle.Width = w;
        _selectionRectangle.Height = h;

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);

        MousePositionText.Text =
            $"[ {(int)(x / _scaleRatio)}, {(int)(y / _scaleRatio)}, {(int)(w / _scaleRatio)}, {(int)(h / _scaleRatio)} ]";
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_selectionRectangle == null)
            return;

        // 释放鼠标捕获
        Mouse.Capture(null);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectionRectangle == null)
        {
            GrowlHelper.WarningGlobal("请选择一个区域");
            return;
        }

        var x = Canvas.GetLeft(_selectionRectangle);
        var y = Canvas.GetTop(_selectionRectangle);
        var w = _selectionRectangle.Width;
        var h = _selectionRectangle.Height;

        GetColorRange(Math.Round(x), Math.Round(y), Math.Round(w), Math.Round(h));
        DialogResult = true;
        Close();
    }

    private void GetColorRange(double x, double y, double width, double height)
    {
        if (width < 1 || !double.IsNormal(width)) width = 1;
        if (height < 1|| !double.IsNormal(height)) height = 1;
        // 创建BitmapImage对象
        if (image.Source is BitmapImage bitmapImage)
        {
            var roiX = Math.Max(x - 5, 0);
            var roiY = Math.Max(y - 5, 0);
            var roiW = Math.Min(width + 10, bitmapImage.PixelWidth - roiX);
            var roiH = Math.Min(height + 10, bitmapImage.PixelHeight - roiY);
            var writeableBitmap = new WriteableBitmap(bitmapImage);

            OutputRoi = [(int)roiX, (int)roiY, (int)roiW, (int)roiH];

            var croppedBitmap =
                new CroppedBitmap(writeableBitmap, new Int32Rect((int)x, (int)y, (int)width, (int)height));

            var pixels = new byte[(int)width * (int)height * 4];
            croppedBitmap.CopyPixels(pixels, (int)width * 4, 0);

            int minR = 255, minG = 255, minB = 255;
            int maxR = 0, maxG = 0, maxB = 0;

            for (int i = 0; i < pixels.Length; i += 4)
            {
                int r = pixels[i + 2];
                int g = pixels[i + 1];
                int b = pixels[i];

                if (r < minR) minR = r;
                if (g < minG) minG = g;
                if (b < minB) minB = b;

                if (r > maxR) maxR = r;
                if (g > maxG) maxG = g;
                if (b > maxB) maxB = b;
            }

            var lower = new List<int> { minR, minG, minB };
            var upper = new List<int> { maxR, maxG, maxB };
            OutputUpper = upper;
            OutputLower = lower;
            // 输出颜色上下限值
        }
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