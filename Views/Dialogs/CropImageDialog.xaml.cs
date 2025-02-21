
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MFAWPF.Helper;
using Microsoft.Win32;

namespace MFAWPF.Views;

public partial class CropImageDialog
{
    private Point _startPoint;
    private Rectangle _selectionRectangle;

    public string Output { get; set; }
    private List<int> _outputRoi;

    public List<int> OutputRoi
    {
        get => _outputRoi;
        set => _outputRoi = value?.Select(i => i < 0 ? 0 : i).ToList();
    }


    public CropImageDialog()
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
        var position = e.GetPosition(image);
        var canvasPosition = e.GetPosition(SelectionCanvas);

        // 判断点击是否在Image边缘5个像素内
        if (canvasPosition.X < image.ActualWidth + 5 && canvasPosition.Y < image.ActualHeight + 5 && canvasPosition is { X: > -5, Y: > -5 })
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
                StrokeDashArray =
                {
                    2
                }
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

        // 确保矩形不会超出右边和下边
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

        var x = Canvas.GetLeft(_selectionRectangle) / _scaleRatio;
        var y = Canvas.GetTop(_selectionRectangle) / _scaleRatio;
        var w = _selectionRectangle.Width / _scaleRatio;
        var h = _selectionRectangle.Height / _scaleRatio;

        SaveCroppedImage((int)x, (int)y, (int)w, (int)h);
    }

    private void SaveCroppedImage(double x, double y, double width, double height)
    {
        // 创建BitmapImage对象
        if (image.Source is BitmapImage bitmapImage)
        {
            var roiX = Math.Max(x - 5, 0);
            var roiY = Math.Max(y - 5, 0);
            var roiW = Math.Min(width + 10, bitmapImage.PixelWidth - roiX);
            var roiH = Math.Min(height + 10, bitmapImage.PixelHeight - roiY);
            OutputRoi = new List<int>
            {
                (int)roiX,
                (int)roiY,
                (int)roiW,
                (int)roiH
            };
            // 创建WriteableBitmap对象并加载BitmapImage
            var writeableBitmap = new WriteableBitmap(bitmapImage);

            // 创建一个用于存储裁剪区域的矩形
            var cropRect = new Int32Rect((int)x, (int)y, (int)width, (int)height);

            // 创建一个字节数组来保存裁剪区域的像素数据
            var croppedPixels = new byte[cropRect.Width * cropRect.Height * 4];
            writeableBitmap.CopyPixels(cropRect, croppedPixels, cropRect.Width * 4, 0);

            // 创建一个新的WriteableBitmap来保存裁剪后的图像
            var croppedBitmap = new WriteableBitmap(cropRect.Width, cropRect.Height, 96, 96, PixelFormats.Bgra32, null);
            croppedBitmap.WritePixels(new Int32Rect(0, 0, cropRect.Width, cropRect.Height), croppedPixels,
                cropRect.Width * 4, 0);

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "ImageFilter".ToLocalization(),
                DefaultExt = "png"
            };

            if (saveFileDialog.ShowDialog().IsTrue())
            {
                var encoder = GetEncoderByExtension(saveFileDialog.FileName);
                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));

                using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                // 设置 Output 属性为保存的文件名和路径
                Output = System.IO.Path.GetFileName(saveFileDialog.FileName);
                DialogResult = true;
                Close();
            }
        }
    }

    private BitmapEncoder GetEncoderByExtension(string fileName)
    {
        var extension = System.IO.Path.GetExtension(fileName).ToLower();

        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                return new JpegBitmapEncoder();
            case ".bmp":
                return new BmpBitmapEncoder();
            default:
                return new PngBitmapEncoder();
        }
    }

    private void Load(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "LoadImageTitle".ToLocalization(),
            Filter = "ImageFilter".ToLocalization()
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
    private void Select(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "LoadImageTitle".ToLocalization(),
            Filter = "ImageFilter".ToLocalization()
        };
        if (openFileDialog.ShowDialog().IsTrue())
        {
            try
            {
                Output = System.IO.Path.GetFileName(openFileDialog.FileName);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorView.ShowException(ex);
            }
        }
    }
}
