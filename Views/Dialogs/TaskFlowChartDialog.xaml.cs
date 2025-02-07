using MFAWPF.Helper;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MFAWPF.ViewModels;
using QuickGraph;
using System.Windows.Data;
using TextBlock = System.Windows.Controls.TextBlock;

namespace MFAWPF.Views;

public partial class TaskFlowChartDialog
{
    private bool _isDragging;
    private Point _startPoint;
    private Dictionary<string, TaskItemViewModel> _vertexTaskMapping;
    private readonly EditTaskDialog _editTaskDialog;
    private Vertex _selectedVertex; // 用于连线的源节点

    public TaskFlowChartDialog(EditTaskDialog editTaskDialog)
    {
        InitializeComponent();
        _editTaskDialog = editTaskDialog;

        UpdateGraph();
    }

    private void Dialog_MouseWheel(object sender, MouseWheelEventArgs e)
    {

        var isCtrlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        if (isCtrlKeyPressed)
        {
            Point mousePosition = e.GetPosition(GraphArea);
            double scaleX = sfr.ScaleX;
            double scaleY = sfr.ScaleY;

            double factor = e.Delta > 0 ? 1.1 : 1 / 1.1;
            scaleX *= factor;
            scaleY *= factor;

            // 更新缩放比例
            sfr.ScaleX = scaleX;
            sfr.ScaleY = scaleY;
            // 检查边界
            CheckZoomBounds(mousePosition, scaleX, scaleY);
        }
    }

    private void CheckZoomBounds(Point mousePosition, double scaleX, double scaleY)
    {
        double imageWidth = graphLayout.ActualWidth;
        double imageHeight = graphLayout.ActualHeight;

        if (mousePosition.X >= 0 && mousePosition.X <= imageWidth)
        {
            sfr.CenterX = mousePosition.X;
        }
        else
        {
            sfr.CenterX = imageWidth;
        }

        if (mousePosition.Y >= 0 && mousePosition.Y <= imageHeight)
        {
            sfr.CenterY = mousePosition.Y;
        }
        else
        {
            sfr.CenterY = imageHeight;
        }
    }
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    private void GraphLayout_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _startPoint = e.GetPosition(GraphArea);
                _isDragging = true;
                Mouse.Capture(GraphArea);
            }
            else
            {
                var hitTestResult = VisualTreeHelper.HitTest(graphLayout, e.GetPosition(graphLayout));
                if (hitTestResult?.VisualHit is TextBlock textBlock && _editTaskDialog.Data is { DataList: not null })
                {
                    _editTaskDialog.Data.CurrentTask =
                        _editTaskDialog.Data.DataList.FirstOrDefault(model =>
                            !string.IsNullOrWhiteSpace(textBlock.Text) && model.Name.Equals(textBlock.Text))
                        ?? _editTaskDialog.Data.CurrentTask;
                }
            }
        }
    }

    private void GraphLayout_MouseMove(object sender, MouseEventArgs e)
    {
        var isCtrlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        if (_isDragging && isCtrlKeyPressed && e.LeftButton == MouseButtonState.Pressed)
        {
            var dposition = e.GetPosition(GraphArea);
            var previousPosition = _startPoint;
            if (previousPosition == new Point(0, 0))
            {
                previousPosition = dposition;
            }

            double offsetX = dposition.X - previousPosition.X;
            double offsetY = dposition.Y - previousPosition.Y;
            
            var translateTransform = ttf;
            translateTransform.X += offsetX;
            translateTransform.Y += offsetY;

            _startPoint = dposition;
        }
    }

    private void GraphLayout_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
        }
        // 释放鼠标捕获
        Mouse.Capture(null);
    }


    public BidirectionalGraph<Vertex, CustomEdge> InternalGraph { get; set; }
    public BidirectionalGraph<object, IEdge<object>> Graph { get; set; }

    public void UpdateGraph()
    {
        var tasks = _editTaskDialog.Data?.DataList;
        if (tasks == null) return;

        InternalGraph = new BidirectionalGraph<Vertex, CustomEdge>();

        // 创建顶点字典
        var vertexDictionary = new Dictionary<string, Vertex>();

        // 添加顶点
        foreach (var task in tasks)
        {
            if (!vertexDictionary.ContainsKey(task.Name))
            {
                var vertex = new Vertex(task.Name);
                vertexDictionary[task.Name] = vertex;
                InternalGraph.AddVertex(vertex);
            }

            if (task.Task is { Next: not null })
            {
                foreach (var nextTask in task.Task.Next)
                {
                    if (!vertexDictionary.ContainsKey(nextTask))
                    {
                        var vertex = new Vertex(nextTask);
                        vertexDictionary[nextTask] = vertex;
                        InternalGraph.AddVertex(vertex);
                    }

                    var source = vertexDictionary[task.Name];
                    var target = vertexDictionary[nextTask];
                    var edge = new CustomEdge(source, target, "Next");
                    InternalGraph.AddEdge(edge);
                }
            }

            // if (task.Task is { OnError: not null })
            // {
            //     foreach (var errorTask in task.Task.OnError)
            //     {
            //         if (!vertexDictionary.ContainsKey(errorTask))
            //         {
            //             var vertex = new Vertex(errorTask);
            //             vertexDictionary[errorTask] = vertex;
            //             InternalGraph.AddVertex(vertex);
            //         }
            //
            //         var source = vertexDictionary[task.Name];
            //         var target = vertexDictionary[errorTask];
            //         var edge = new CustomEdge(source, target, "OnError");
            //         InternalGraph.AddEdge(edge);
            //     }
            // }

            // 进行类型适配
            var adaptedVertices = InternalGraph.Vertices.Cast<object>().ToList();
            var adaptedEdges = new List<IEdge<object>>();
            foreach (var edge in InternalGraph.Edges)
            {
                adaptedEdges.Add(new EdgeWithLabel(edge.Source, edge.Target, edge.EdgeType));
            }

            Graph = new BidirectionalGraph<object, IEdge<object>>();
            foreach (var vertex in adaptedVertices)
            {
                Graph.AddVertex(vertex);
            }
            foreach (var edge in adaptedEdges)
            {
                Graph.AddEdge(edge);
            }


        }
        graphLayout.Graph = Graph;
    }

    public class EdgeWithLabel : Edge<object>
    {
        public string EdgeType { get; private set; }

        public EdgeWithLabel(object source, object target, string edgeType)
            : base(source, target)
        {
            EdgeType = edgeType;
        }

        public override string ToString()
        {
            return $"{Source} -> {Target} ({EdgeType})";
        }
    }

    public class Vertex
    {
        public string Name { get; set; }

        public Vertex(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class CustomEdge : Edge<Vertex>
    {
        public string EdgeType { get; set; }

        public CustomEdge(Vertex source, Vertex target, string edgeType) : base(source, target)
        {
            EdgeType = edgeType;
        }
    }
    // 边类型到画刷的转换器
}
