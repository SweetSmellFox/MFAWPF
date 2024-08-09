using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MFAWPF.Controls;
using MFAWPF.ViewModels;
using QuickGraph;
using GraphSharp.Controls;
using MFAWPF.Views;
using TextBlock = System.Windows.Controls.TextBlock;

namespace MFAWPF.Views
{
    public partial class TaskFlowChartDialog : CustomWindow
    {
        private bool _isDragging;
        private Point _startPoint;
        private readonly Dictionary<string, TaskItemViewModel> _vertexTaskMapping;
        private EditTaskDialog EditTaskDialog;

        public TaskFlowChartDialog(EditTaskDialog editTaskDialog, List<TaskItemViewModel> taskModels) :
            base()
        {
            InitializeComponent();
            _vertexTaskMapping = new Dictionary<string, TaskItemViewModel>();
            var graph = CreateGraph(taskModels);
            graphLayout.Graph = graph;
            EditTaskDialog = editTaskDialog;
        }

        private QuickGraph.BidirectionalGraph<object, QuickGraph.IEdge<object>> CreateGraph(
            List<TaskItemViewModel> tasks)
        {
            var graph = new QuickGraph.BidirectionalGraph<object, QuickGraph.IEdge<object>>();

            var vertexDictionary = new Dictionary<string, Vertex>();

            foreach (var task in tasks)
            {
                if (!vertexDictionary.ContainsKey(task.Name))
                {
                    var vertex = new Vertex(task.Name);
                    vertexDictionary[task.Name] = vertex;
                    graph.AddVertex(vertex);
                    _vertexTaskMapping[vertex.Name] = task;
                }

                if (task.Task != null)
                {
                    AddEdges(graph, vertexDictionary, task.Task.next, vertexDictionary[task.Name], "Normal");
                    // AddEdges(graph, vertexDictionary, task.Task.timeout_next, vertexDictionary[task.Name], "Timeout");
                    // AddEdges(graph, vertexDictionary, task.Task.runout_next, vertexDictionary[task.Name], "Runout");
                }
            }

            return graph;
        }

        private void AddEdges(BidirectionalGraph<object, IEdge<object>> graph,
            Dictionary<string, Vertex> vertexDictionary, List<string> nextTasks, Vertex sourceVertex, string edgeType)
        {
            if (nextTasks != null)
            {
                foreach (var nextTask in nextTasks)
                {
                    if (!vertexDictionary.ContainsKey(nextTask))
                    {
                        var vertex = new Vertex(nextTask);
                        vertexDictionary[nextTask] = vertex;
                        graph.AddVertex(vertex);
                    }

                    var edge = new EdgeWithLabel(sourceVertex, vertexDictionary[nextTask], edgeType);
                    graph.AddEdge(edge);
                }
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

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GraphLayout_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void GraphLayout_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPoint = e.GetPosition(scrollViewer);
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - (currentPoint.X - _startPoint.X));
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (currentPoint.Y - _startPoint.Y));
                _startPoint = currentPoint;
            }
        }

        private void GraphLayout_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _isDragging)
            {
                _isDragging = false;
                Mouse.Capture(null);
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    graphLayout.LayoutTransform = new ScaleTransform(graphLayout.LayoutTransform.Value.M11 * 1.1,
                        graphLayout.LayoutTransform.Value.M22 * 1.1);
                }
                else
                {
                    graphLayout.LayoutTransform = new ScaleTransform(graphLayout.LayoutTransform.Value.M11 / 1.1,
                        graphLayout.LayoutTransform.Value.M22 / 1.1);
                }

                e.Handled = true;
            }
        }

        private void GraphLayout_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    _startPoint = e.GetPosition(scrollViewer);
                    _isDragging = true;
                    Mouse.Capture(graphLayout);
                }
                else
                {
                    var hitTestResult = VisualTreeHelper.HitTest(graphLayout, e.GetPosition(graphLayout));
                    if (hitTestResult != null)
                    {
                        if (hitTestResult.VisualHit is TextBlock textBlock)
                        {
                            EditTaskDialog.Data.CurrentTask =
                                EditTaskDialog.Data.DataList.Where(model =>
                                        !string.IsNullOrWhiteSpace(textBlock.Text) && model.Name.Equals(textBlock.Text))
                                    .FirstOrDefault() ?? EditTaskDialog.Data.CurrentTask;
                        }
                    }
                }
            }
        }
    }
}