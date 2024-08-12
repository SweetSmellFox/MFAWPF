using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MFAWPF.Controls;
using MFAWPF.Utils;

namespace MFAWPF.Views
{
    public partial class RecognitionTextDialog : CustomWindow
    {
        private Point _startPoint;
        private Rectangle? _selectionRectangle;
        public List<int>? Output { get; set; }
        public List<int>? OutputRoi { get; set; }

        public RecognitionTextDialog(BitmapImage bitmapImage) : base()
        {
            InitializeComponent();
            UpdateImage(bitmapImage);
        }

        private double _scaleRatio;

        private void UpdateImage(BitmapImage _imageSource)
        {
            image.Source = _imageSource;
            Console.WriteLine($"{_imageSource.PixelWidth},{_imageSource.PixelHeight}");

            double imageWidth = _imageSource.PixelWidth;
            double imageHeight = _imageSource.PixelHeight;

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
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(image);
            var canvasPosition = e.GetPosition(SelectionCanvas);

            // 判断点击是否在Image边缘5个像素内
            if (canvasPosition.X < image.ActualWidth + 5 && canvasPosition.Y < image.ActualHeight + 5 &&
                canvasPosition.X > -5 && canvasPosition.Y > -5)
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
                    StrokeDashArray = new DoubleCollection { 2 }
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
                Growls.WarningGlobal("请选择一个区域");
                return;
            }

            var x = (int)(Canvas.GetLeft(_selectionRectangle) / _scaleRatio);
            var y = (int)(Canvas.GetTop(_selectionRectangle) / _scaleRatio);
            var w = (int)(_selectionRectangle.Width / _scaleRatio);
            var h = (int)(_selectionRectangle.Height / _scaleRatio);

            Output = new() { x, y, w, h };
            if (image.Source is BitmapImage bitmapImage)
            {
                var roiX = Math.Max(x - 5, 0);
                var roiY = Math.Max(y - 5, 0);
                var roiW = Math.Min(w + 10, bitmapImage.PixelWidth - roiX);
                var roiH = Math.Min(h + 10, bitmapImage.PixelHeight - roiY);
                OutputRoi = new List<int> { (int)roiX, (int)roiY, (int)roiW, (int)roiH };
            }
            DialogResult = true;
            Close();
        }

        protected override void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}