using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MFAWPF.Controls;
using MFAWPF.ViewModels;
using QuickGraph;
using GraphSharp.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace MFAWPF.Views
{
    public partial class TaskFlowChartDialog : CustomWindow
    {
        private bool _isDragging;
        private Point _startPoint;
        private Dictionary<string, TaskItemViewModel>? _vertexTaskMapping;
        private readonly EditTaskDialog _editTaskDialog;
        private Vertex? _selectedVertex; // 用于连线的源节点

        public TaskFlowChartDialog(EditTaskDialog editTaskDialog) : base()
        {
            InitializeComponent();
            _editTaskDialog = editTaskDialog;

            UpdateGraph();
        }

        private BidirectionalGraph<object, IEdge<object>> CreateGraph(List<TaskItemViewModel>? tasks)
        {
            var graph = new BidirectionalGraph<object, IEdge<object>>();
            var vertexDictionary = new Dictionary<string, Vertex>();
            if (tasks == null || tasks.Count == 0)
                return graph;
            foreach (var task in tasks)
            {
                if (_vertexTaskMapping != null && !vertexDictionary.ContainsKey(task.Name))
                {
                    var vertex = new Vertex(task.Name);
                    vertexDictionary[task.Name] = vertex;
                    graph.AddVertex(vertex);
                    Console.WriteLine(vertex.Name);
                    _vertexTaskMapping[vertex.Name] = task;
                }

                if (task.Task != null && task.Task?.next != null)
                {
                    AddEdges(graph, vertexDictionary, task.Task.next, vertexDictionary[task.Name], "Normal");
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

        protected override void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GraphLayout_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // if (e.ChangedButton == MouseButton.Right)
            // {
            //     // 右键删除节点或连线
            //     var hitTestResult = VisualTreeHelper.HitTest(graphLayout, e.GetPosition(graphLayout));
            //     if (hitTestResult != null && hitTestResult.VisualHit is TextBlock textBlock)
            //     {
            //         var taskName = textBlock.Text;
            //
            //         // 改进后的部分：先检查键是否存在
            //         if (_vertexTaskMapping.TryGetValue(taskName, out var taskToRemove))
            //         {
            //             _editTaskDialog.Data.DataList.Remove(taskToRemove);
            //             UpdateGraph();
            //         }
            //         else
            //         {
            //             // 键不存在的情况，处理错误或警告
            //             MessageBox.Show($"任务 '{taskName}' 未找到。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //         }
            //     }
            //     else if (hitTestResult.VisualHit is Path line)
            //     {
            //         var edge = line.DataContext as EdgeWithLabel;
            //         if (edge != null)
            //         {
            //             var sourceTask = _vertexTaskMapping[edge.Source.ToString()];
            //             sourceTask.Task.next.Remove(edge.Target.ToString());
            //             UpdateGraph();
            //         }
            //     }
            // }
        }

        private void GraphLayout_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPoint = e.GetPosition(scrollViewer);
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset -
                                                      (currentPoint.X - _startPoint.X));
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

        public void UpdateGraph()
        {
            _vertexTaskMapping = new Dictionary<string, TaskItemViewModel>();
            var graph = CreateGraph(_editTaskDialog?.Data?.DataList?.ToList());
            graphLayout.Graph = graph;
            if (_editTaskDialog?.Data != null)
                _editTaskDialog.Data.CurrentTask = null; // 更新完图形后，清空选择以防止不必要的误操作
        }
    }
}