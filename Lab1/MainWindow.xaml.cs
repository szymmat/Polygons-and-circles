using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Lab1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<Point> clickedPoints = new();
        private bool editMode;
        private readonly List<List<Rectangle>> polygons = new();
        private readonly List<List<Edge>> edges = new();
        private readonly List<Ellipse> ellipses = new();
        private Ellipse selectedEllipse;
        private List<Rectangle> selectedPolygon;
        private List<Rectangle> additionalSelectedPolygon;
        private readonly List<Edge> selectedEdges = new();
        private Rectangle selectedVertex;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SetPixel(int x, int y, Color color, bool isVertex, bool remove, Edge edge)
        {
            int size = isVertex ? 10 : 1;
            Rectangle rectangle = new();
            rectangle.SetValue(Canvas.LeftProperty, (double)x);
            rectangle.SetValue(Canvas.TopProperty, (double)y);
            rectangle.Width = size;
            rectangle.Height = size;
            rectangle.Fill = new SolidColorBrush(color);
            if (isVertex) rectangle.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;
            else if (!remove)
            {
                rectangle.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown1;
                edge.Pixels.Add(rectangle);
            }
            if (!remove) canvas.Children.Add(rectangle);
            else
            {
                try
                {
                    Rectangle rect = canvas.Children.OfType<UIElement>().First(r => (double)r.GetValue(Canvas.TopProperty) == y && (double)r.GetValue(Canvas.LeftProperty) == x) as Rectangle;
                    edge.Pixels.Remove(rect);
                    canvas.Children.Remove(rect);
                }
                catch (InvalidOperationException)
                {

                }
            }
        }

        private void Rectangle_MouseLeftButtonDown1(object sender, MouseButtonEventArgs e)
        {
            Rectangle rectangle = sender as Rectangle;
            foreach (var polygon in edges)
            {
                foreach (var edge in polygon)
                {
                    foreach (var px in edge.Pixels)
                    {
                        if ((double)px.GetValue(Canvas.LeftProperty) == (double)rectangle.GetValue(Canvas.LeftProperty)
                            && (double)px.GetValue(Canvas.TopProperty) == (double)rectangle.GetValue(Canvas.TopProperty))
                        {
                            if (selectedEdges.Count == 0)
                            {
                                EdgeStackPanel.IsEnabled = true;
                                EdgeRestrictionsStackPanel.IsEnabled = true;
                                if (selectedEllipse != null)
                                {
                                    radioTangent.IsEnabled = true;
                                }
                                else
                                {
                                    radioTangent.IsEnabled = false;
                                    radioTangent.IsChecked = false;
                                }
                            }
                            else if (selectedEdges.Count == 1)
                            {
                                EdgePairStackPanel.IsEnabled = true;
                                EdgeStackPanel.IsEnabled = false;
                                EdgeRestrictionsStackPanel.IsEnabled = false;
                                radioTangent.IsEnabled = false;
                                radioTangent.IsChecked = false;
                            }
                            else if (selectedEdges.Count == 2)
                            {
                                selectedEdges[0].Pixels.ForEach(ee =>
                                {
                                    ee.Fill = selectedEdges[0].ConstLength.HasValue
                                        ? new SolidColorBrush(Colors.Green)
                                        : selectedEdges[0].TangentTo != null ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Blue);
                                    if (selectedEdges[0].PerpendicularTo != null) ee.Fill = new SolidColorBrush(Colors.Aqua);
                                });
                                selectedEdges.RemoveAt(0);
                                radioTangent.IsEnabled = false;
                                radioTangent.IsChecked = false;
                            }
                            selectedEdges.Add(edge);
                            if (selectedPolygon != null && additionalSelectedPolygon != null)
                            {
                                selectedPolygon = additionalSelectedPolygon;
                                additionalSelectedPolygon = polygons[edges.IndexOf(polygon)];
                            }
                            else if (selectedPolygon != null && additionalSelectedPolygon == null)
                            {
                                additionalSelectedPolygon = polygons[edges.IndexOf(polygon)];
                            }
                            else selectedPolygon = polygons[edges.IndexOf(polygon)];

                        }
                    }
                }
            }
            selectedEdges.ForEach(e => e.Pixels.ForEach(p =>
            {
                canvas.Children.Remove(p);
                p.Fill = new SolidColorBrush(Colors.Red);
                canvas.Children.Add(p);
            }));
            VertexStackPanel.IsEnabled = false;
            PolygonStackPanel.IsEnabled = false;
            e.Handled = true;
        }

        private Edge BresenhamLineDrawing(int x1, int y1, int x2, int y2, bool remove)
        {
            Edge edge = new();
            (int x, int y) = (x1, y1);
            (int dx, int dy) = (Math.Abs(x2 - x1), Math.Abs(y2 - y1));
            (int s1, int s2) = (Math.Sign(x2 - x1), Math.Sign(y2 - y1));
            int interchange = 0;
            if (dy > dx)
            {
                (dy, dx) = (dx, dy);
                interchange = 1;
            }
            int e = 2 * dy - dx;
            int a = 2 * dy;
            int b = 2 * dy - 2 * dx;
            SetPixel(x, y, Colors.Black, true, remove, edge);
            for (int i = 1; i <= dx; i++)
            {
                if (e < 0)
                {
                    if (interchange == 1)
                    {
                        y += s2;
                    }
                    else
                    {
                        x += s1;
                    }
                    e += a;
                }
                else
                {
                    y += s2;
                    x += s1;
                    e += b;
                }
                SetPixel(x, y, Colors.Blue, false, remove, edge);
            }
            return edge;
        }

        private void btnPolygon_Click(object sender, RoutedEventArgs e)
        {
            if (editMode == false)
            {
                editMode = true;
                btnPolygon.Content = "End drawing";
            }
            else
            {
                List<Rectangle> polygon = new();
                List<Edge> drawnEdges = new();
                if (clickedPoints.Count < 3)
                {
                    MessageBox.Show("Choose at least 3 vertices.");
                }
                else
                {
                    for(int i=0;i<clickedPoints.Count;i++)
                    {
                        polygon.Add(CreateVertex(Convert.ToInt32(clickedPoints[i].X), Convert.ToInt32(clickedPoints[i].Y)));
                    }
                    for (int i = 0; i < clickedPoints.Count - 1; i++)
                    {
                        drawnEdges.Add(BresenhamLineDrawing(Convert.ToInt32(Canvas.GetLeft(polygon[i])), Convert.ToInt32(Canvas.GetTop(polygon[i])), Convert.ToInt32(Canvas.GetLeft(polygon[i+1])), Convert.ToInt32(Canvas.GetTop(polygon[i+1])), false));
                    }
                    drawnEdges.Add(BresenhamLineDrawing(Convert.ToInt32(Canvas.GetLeft(polygon[^1])), Convert.ToInt32(Canvas.GetTop(polygon[^1])), Convert.ToInt32(Canvas.GetLeft(polygon[0])), Convert.ToInt32(Canvas.GetTop(polygon[0])), false));
                }
                polygons.Add(polygon);
                edges.Add(drawnEdges);
                editMode = false;
                clickedPoints.Clear();
                btnPolygon.Content = "New polygon";
            }

        }

        private static (int?, int?) ParseCoords(TextBox txtX, TextBox txtY)
        {
            int? x = null, y = null;
            try
            {
                x = int.Parse(txtX.Text);
            }
            catch (Exception)
            {

            }
            try
            {
                y = int.Parse(txtY.Text);
            }
            catch (Exception)
            {

            }
            return (x, y);
        }

        private static (int?, int?, int?) ParseCoords(TextBox txtX, TextBox txtY, TextBox txtRadius)
        {
            int? x = null, y = null, r = null;
            try
            {
                x = int.Parse(txtX.Text);
            }
            catch (Exception)
            {

            }
            try
            {
                y = int.Parse(txtY.Text);
            }
            catch (Exception)
            {

            }
            try
            {
                r = int.Parse(txtRadius.Text);
            }
            catch (Exception)
            {

            }
            return (x, y, r);
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (editMode)
            {
                clickedPoints.Add(e.GetPosition(canvas));
                return;
            }
            VertexStackPanel.IsEnabled = false;
            EdgeStackPanel.IsEnabled = false;
            PolygonStackPanel.IsEnabled = false;
            CircleStackPanel.IsEnabled = false;
            EdgePairStackPanel.IsEnabled = false;
            EdgeRestrictionsStackPanel.IsEnabled = false;
            selectedEllipse = null;
            selectedPolygon = null;
            additionalSelectedPolygon = null;
            selectedVertex = null;
            radioTangent.IsEnabled = false;
            radioTangent.IsChecked = false;
            selectedEdges.Clear();
            ellipses.ForEach(e => e.Effect = null);
            foreach (var polygon in polygons)
            {
                foreach (Rectangle vertex in polygon)
                {
                    canvas.Children.Remove(vertex);
                    vertex.Fill = new SolidColorBrush(Colors.Black);
                    canvas.Children.Add(vertex);
                }
            }
            edges.ForEach(p => p.ForEach(e =>
            {
                if (e.ConstLength.HasValue)
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Green));
                }
                else if (e.TangentTo != null)
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Orange));
                }
                else if (e.PerpendicularTo != null)
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Aqua));
                }
                else
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Blue));
                }

            }));
        }

        private void btnCircle_Click(object sender, RoutedEventArgs e)
        {
            WindowCircle windowCircle = new();
            if (windowCircle.ShowDialog() == true)
            {
                (int? x, int? y, int? r) = ParseCoords(windowCircle.txtCenterX, windowCircle.txtCenterY, windowCircle.txtRadius);
                if (!x.HasValue || !y.HasValue || !r.HasValue) return;
                Ellipse ellipse = createEllipse(x, y, r);
                ellipses.Add(ellipse);
                canvas.Children.Add(ellipse);
            }
        }

        private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            VertexStackPanel.IsEnabled = false;
            EdgeStackPanel.IsEnabled = false;
            PolygonStackPanel.IsEnabled = false;
            CircleStackPanel.IsEnabled = false;
            EdgePairStackPanel.IsEnabled = false;
            EdgeRestrictionsStackPanel.IsEnabled = false;
            selectedEllipse = null;
            selectedVertex = null;
            if (selectedEdges.Count == 1)
            {
                radioTangent.IsEnabled = true;
                radioTangent.IsChecked = false;
                EdgeStackPanel.IsEnabled = true;
                EdgeRestrictionsStackPanel.IsEnabled = true;
            }
            else
            {
                selectedEdges.Clear();
                selectedPolygon = null;
                additionalSelectedPolygon = null;
            }
            ellipses.ForEach(e => e.Effect = null);
            Ellipse selection = sender as Ellipse;
            DropShadowEffect dropShadowEffect = new()
            {
                BlurRadius = 50,
                Direction = 270,
                Color = Colors.Red
            };
            selection.Effect = dropShadowEffect;
            selectedEllipse = selection;
            CircleStackPanel.IsEnabled = true;
            e.Handled = true;
        }

        private void btnDeleteCircle_Click(object sender, RoutedEventArgs e)
        {
            edges.ForEach(p => p.ForEach(e =>
            {
                if (e.TangentTo != null && e.TangentTo == selectedEllipse)
                {
                    e.TangentTo = null;
                    e.Pixels.ForEach(px =>
                    {
                        canvas.Children.Remove(px);
                        px.Fill = new SolidColorBrush(Colors.Blue);
                        canvas.Children.Add(px);
                    });
                }
            }));
            canvas.Children.Remove(selectedEllipse);
            ellipses.Remove(selectedEllipse);
            CircleStackPanel.IsEnabled = false;
        }

        private void btnEditCircle_Click(object sender, RoutedEventArgs e)
        {
            (int? x, int? y, int? r) = ParseCoords(txtCircleOffsetX, txtCircleOffsetY, txtCircleChangeRadius);
            int oldRadius = Convert.ToInt32(selectedEllipse.Width / 2);
            (int offsetX, int offsetY) = (Convert.ToInt32(x - Canvas.GetLeft(selectedEllipse) - selectedEllipse.Width / 2),
                Convert.ToInt32(y - Canvas.GetTop(selectedEllipse) - selectedEllipse.Height / 2));
            List<List<Rectangle>> polygonsToMove = new();
            HashSet<Ellipse> ellipsesToMove = new();
            foreach (var polygon in edges)
            {
                foreach (Edge edge in polygon)
                {
                    if (edge.TangentTo == selectedEllipse)
                    {
                        selectedPolygon = polygons[edges.IndexOf(polygon)];
                        polygonsToMove.Add(selectedPolygon);
                    }
                }
            }
            for (int i = 0; i < polygonsToMove.Count; i++)
            {
                int idx = polygons.IndexOf(polygonsToMove[i]);
                foreach (Edge edge in edges[idx])
                {
                    if (edge.TangentTo != null && edge.TangentTo != selectedEllipse) ellipsesToMove.Add(edge.TangentTo);
                    if (edge.PerpendicularTo != null)
                    {
                        for (int j = 0; j < edges.Count; j++)
                        {
                            if (edges[j].Contains(edge.PerpendicularTo) && !polygonsToMove.Contains(polygons[j])) polygonsToMove.Add(polygons[j]);
                        }
                    }
                }
            }
            MoveCircle(x, y, r);
            foreach (var polygon in polygonsToMove)
            {
                selectedPolygon = polygon;
                MovePolygon(offsetX, Convert.ToInt32(offsetY + oldRadius - r));
            }
            foreach (Ellipse ellipse in ellipsesToMove)
            {
                selectedEllipse = ellipse;
                x ??= 0;
                y ??= 0;
                MoveCircle(Convert.ToInt32(Canvas.GetLeft(ellipse) + ellipse.Width / 2 + offsetX), Convert.ToInt32(Canvas.GetTop(ellipse) + ellipse.Height / 2 + offsetY), Convert.ToInt32(ellipse.Height / 2));
            }
        }

        private void btnDeletePolygon_Click(object sender, RoutedEventArgs e)
        {
            int idx = polygons.IndexOf(selectedPolygon);
            for (int i = 0; i < selectedPolygon.Count - 1; i++)
            {
                foreach (var polygon in edges)
                {
                    foreach (Edge edge in polygon)
                    {
                        if (edge.PerpendicularTo == edges[idx][i])
                        {
                            edge.PerpendicularTo = null;
                            edge.Pixels.ForEach(px =>
                            {
                                canvas.Children.Remove(px);
                                px.Fill = new SolidColorBrush(Colors.Blue);
                                canvas.Children.Add(px);
                            });
                        }
                    }
                }
                BresenhamLineDrawing(Convert.ToInt32(selectedPolygon[i].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[i].GetValue(Canvas.TopProperty)), Convert.ToInt32(selectedPolygon[i + 1].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[i + 1].GetValue(Canvas.TopProperty)), true);
                canvas.Children.Remove(selectedPolygon[i]);
            }
            BresenhamLineDrawing(Convert.ToInt32(selectedPolygon[^1].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[^1].GetValue(Canvas.TopProperty)), Convert.ToInt32(selectedPolygon[0].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[0].GetValue(Canvas.TopProperty)), true);
            canvas.Children.Remove(selectedPolygon[^1]);
            polygons.Remove(selectedPolygon);
            edges.RemoveAt(idx);
            selectedPolygon = null;
            selectedVertex = null;
            VertexStackPanel.IsEnabled = false;
            PolygonStackPanel.IsEnabled = false;
        }

        private void btnEditPolygon_Click(object sender, RoutedEventArgs e)
        {
            (int? x, int? y) = ParseCoords(txtPolygonOffsetX, txtPolygonOffsetY);
            List<List<Rectangle>> polygonsToMove = new();
            HashSet<Ellipse> ellipsesToMove = new();
            int idx = polygons.IndexOf(selectedPolygon);
            polygonsToMove.Add(selectedPolygon);
            foreach (Edge edge in edges[idx])
            {
                if (edge.TangentTo != null) ellipsesToMove.Add(edge.TangentTo);
                if (edge.PerpendicularTo != null)
                {
                    for (int i = 0; i < edges.Count; i++)
                    {
                        if (edges[i].Contains(edge.PerpendicularTo) && !polygonsToMove.Contains(polygons[i])) polygonsToMove.Add(polygons[i]);
                    }
                }
            }
            for (int i = 0; i < polygonsToMove.Count; i++)
            {
                idx = polygons.IndexOf(polygonsToMove[i]);
                foreach (Edge edge in edges[idx])
                {
                    if (edge.TangentTo != null) ellipsesToMove.Add(edge.TangentTo);
                    if (edge.PerpendicularTo != null)
                    {
                        for (int j = 0; j < edges.Count; j++)
                        {
                            if (edges[j].Contains(edge.PerpendicularTo) && !polygonsToMove.Contains(polygons[j])) polygonsToMove.Add(polygons[j]);
                        }
                    }
                }
            }
            foreach (var polygon in polygonsToMove)
            {
                selectedPolygon = polygon;
                MovePolygon(x, y);
            }
            foreach (Ellipse ellipse in ellipsesToMove)
            {
                selectedEllipse = ellipse;
                x ??= 0;
                y ??= 0;
                MoveCircle(Convert.ToInt32(Canvas.GetLeft(ellipse) + ellipse.Width / 2 + x), Convert.ToInt32(Canvas.GetTop(ellipse) + ellipse.Height / 2 + y), Convert.ToInt32(ellipse.Height / 2));
            }
            selectedPolygon = null;
            selectedEllipse = null;
        }

        private void btnDeleteVertex_Click(object sender, RoutedEventArgs e)
        {
            int idx = polygons.IndexOf(selectedPolygon);
            foreach (Rectangle rectangle in selectedPolygon)
            {
                if ((double)selectedVertex.GetValue(Canvas.LeftProperty) == (double)rectangle.GetValue(Canvas.LeftProperty)
                        && (double)selectedVertex.GetValue(Canvas.TopProperty) == (double)rectangle.GetValue(Canvas.TopProperty))
                {
                    selectedVertex = rectangle;
                }
            }
            if (polygons[idx].Count == 3)
            {
                MessageBox.Show("A polygon must have at least 3 vertices. Cannot remove vertex.");
                return;
            }
            int vertexIndex = selectedPolygon.IndexOf(selectedVertex);
            int firstIdx = vertexIndex == 0 ? selectedPolygon.Count - 1 : vertexIndex - 1;
            int secondIdx = vertexIndex == selectedPolygon.Count - 1 ? 0 : vertexIndex + 1;
            foreach (var polygon in edges)
            {
                foreach (var edge in polygon)
                {
                    if (edge.PerpendicularTo == edges[idx][firstIdx] || edge.PerpendicularTo == edges[idx][vertexIndex])
                    {
                        edge.PerpendicularTo = null;
                        edge.Pixels.ForEach(px =>
                        {
                            canvas.Children.Remove(px);
                            px.Fill = new SolidColorBrush(Colors.Blue);
                            canvas.Children.Add(px);
                        });
                    }
                }
            }
            Rectangle firstNbr = selectedPolygon[firstIdx];
            Rectangle secondNbr = selectedPolygon[secondIdx];
            BresenhamLineDrawing(Convert.ToInt32(firstNbr.GetValue(Canvas.LeftProperty)), Convert.ToInt32(firstNbr.GetValue(Canvas.TopProperty)), Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.TopProperty)), true);
            BresenhamLineDrawing(Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.TopProperty)), Convert.ToInt32(secondNbr.GetValue(Canvas.LeftProperty)), Convert.ToInt32(secondNbr.GetValue(Canvas.TopProperty)), true);
            edges[idx][firstIdx] = BresenhamLineDrawing(Convert.ToInt32(firstNbr.GetValue(Canvas.LeftProperty)), Convert.ToInt32(firstNbr.GetValue(Canvas.TopProperty)), Convert.ToInt32(secondNbr.GetValue(Canvas.LeftProperty)), Convert.ToInt32(secondNbr.GetValue(Canvas.TopProperty)), false);
            canvas.Children.Remove(selectedVertex);
            edges[idx].RemoveAt(vertexIndex);
            selectedPolygon.Remove(selectedVertex);
            selectedVertex = null;
            selectedPolygon = null;
            VertexStackPanel.IsEnabled = false;
            PolygonStackPanel.IsEnabled = false;
            EdgeStackPanel.IsEnabled = false;
            EdgeRestrictionsStackPanel.IsEnabled = false;
        }

        private void btnEditVertex_Click(object sender, RoutedEventArgs e)
        {
            (int? x, int? y) = ParseCoords(txtVertexOffsetX, txtVertexOffsetY);
            HashSet<List<Rectangle>> polygonsToMove = new();
            HashSet<Ellipse> ellipsesToMove = new();
            int idx = polygons.IndexOf(selectedPolygon);
            foreach (Edge edge in edges[idx])
            {
                if (edge.ConstLength.HasValue || edge.TangentTo != null || edge.PerpendicularTo != null) polygonsToMove.Add(selectedPolygon);
                if (edge.TangentTo != null) ellipsesToMove.Add(edge.TangentTo);
                if (edge.PerpendicularTo != null)
                {
                    for (int i = 0; i < edges.Count; i++)
                    {
                        if (edges[i].Contains(edge.PerpendicularTo)) polygonsToMove.Add(polygons[i]);
                    }
                }
            }
            if (polygonsToMove.Count == 0 && ellipsesToMove.Count == 0) MoveVertex(x, y);
            else
            {
                foreach (var polygon in polygonsToMove)
                {
                    selectedPolygon = polygon;
                    MovePolygon(x, y);
                }
                foreach (Ellipse ellipse in ellipsesToMove)
                {
                    selectedEllipse = ellipse;
                    x ??= 0;
                    y ??= 0;
                    MoveCircle(Convert.ToInt32(Canvas.GetLeft(ellipse) + ellipse.Width / 2 + x), Convert.ToInt32(Canvas.GetTop(ellipse) + ellipse.Height / 2 + y), Convert.ToInt32(ellipse.Height / 2));
                }
                selectedPolygon = null;
                selectedEllipse = null;
            }
        }

        private void btnSubdivisionEdge_Click(object sender, RoutedEventArgs e)
        {
            int polygonIdx = polygons.IndexOf(selectedPolygon);
            Edge selectedEdge = selectedEdges[0];
            int edgeIdx = edges[polygonIdx].IndexOf(selectedEdge);
            int nextIdx = (edgeIdx + 1) % selectedPolygon.Count;
            selectedEdge.Pixels.ForEach(e => e.Fill = new SolidColorBrush(Colors.Blue));
            int len = selectedEdge.Pixels.Count;
            Rectangle midRectangle = selectedEdge.Pixels[len / 2];
            Rectangle newRect = CreateVertex(Convert.ToInt32(Canvas.GetLeft(midRectangle)), Convert.ToInt32(Canvas.GetTop(midRectangle)));
            BresenhamLineDrawing(Convert.ToInt32(Canvas.GetLeft(selectedPolygon[edgeIdx])), Convert.ToInt32(Canvas.GetTop(selectedPolygon[edgeIdx])), Convert.ToInt32(Canvas.GetLeft(selectedPolygon[nextIdx])), Convert.ToInt32(Canvas.GetTop(selectedPolygon[nextIdx])), true);
            edges[polygonIdx][edgeIdx] = BresenhamLineDrawing(Convert.ToInt32(Canvas.GetLeft(selectedPolygon[edgeIdx])), Convert.ToInt32(Canvas.GetTop(selectedPolygon[edgeIdx])), Convert.ToInt32(Canvas.GetLeft(newRect)), Convert.ToInt32(Canvas.GetTop(newRect)), false);
            Edge newEdge = BresenhamLineDrawing(Convert.ToInt32(Canvas.GetLeft(newRect)), Convert.ToInt32(Canvas.GetTop(newRect)), Convert.ToInt32(Canvas.GetLeft(selectedPolygon[nextIdx])), Convert.ToInt32(Canvas.GetTop(selectedPolygon[nextIdx])), false);
            canvas.Children.Add(newRect);
            selectedPolygon.Insert(edgeIdx + 1, newRect);
            edges[polygonIdx].Insert(edgeIdx + 1, newEdge);
            selectedEdges.Clear();
            EdgeStackPanel.IsEnabled = false;
            EdgeRestrictionsStackPanel.IsEnabled = false;
        }

        private Rectangle CreateVertex(int x, int y)
        {
            Rectangle rectangle = new()
            {
                Width = 10,
                Height = 10
            };
            rectangle.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;
            rectangle.Fill = new SolidColorBrush(Colors.Black);
            rectangle.SetValue(Canvas.LeftProperty, (double)x);
            rectangle.SetValue(Canvas.TopProperty, (double)y);
            return rectangle;
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle rectangle = sender as Rectangle;
            selectedVertex = rectangle;
            additionalSelectedPolygon = null;
            foreach (var polygon in polygons)
            {
                foreach (Rectangle rectangle1 in polygon)
                {
                    if ((double)rectangle1.GetValue(Canvas.LeftProperty) == (double)rectangle.GetValue(Canvas.LeftProperty)
                        && (double)rectangle1.GetValue(Canvas.TopProperty) == (double)rectangle.GetValue(Canvas.TopProperty))
                    {
                        selectedPolygon = polygon;
                    }
                }
            }
            polygons.ForEach(p => p.ForEach(r => r.Fill = new SolidColorBrush(Colors.Black)));
            rectangle.Fill = new SolidColorBrush(Colors.Red);
            edges.ForEach(p => p.ForEach(e =>
            {
                if (e.ConstLength.HasValue)
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Green));
                }
                else if (e.TangentTo != null)
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Orange));
                }
                else if (e.PerpendicularTo != null)
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Aqua));
                }
                else
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Blue));
                }

            }));
            selectedEdges.Clear();
            VertexStackPanel.IsEnabled = true;
            PolygonStackPanel.IsEnabled = true;
            EdgeStackPanel.IsEnabled = false;
            EdgeRestrictionsStackPanel.IsEnabled = false;
            CircleStackPanel.IsEnabled = false;
            EdgePairStackPanel.IsEnabled = false;
            radioTangent.IsEnabled = false;
            radioTangent.IsChecked = false;
            e.Handled = true;
        }

        private void btnEditEdge_Click(object sender, RoutedEventArgs e)
        {
            (int? x, int? y) = ParseCoords(txtEdgeOffsetX, txtEdgeOffsetY);
            HashSet<List<Rectangle>> polygonsToMove = new();
            HashSet<Ellipse> ellipsesToMove = new();
            int idx = polygons.IndexOf(selectedPolygon);
            foreach (Edge edge in edges[idx])
            {
                if (edge.ConstLength.HasValue || edge.TangentTo != null || edge.PerpendicularTo != null) polygonsToMove.Add(selectedPolygon);
                if (edge.TangentTo != null) ellipsesToMove.Add(edge.TangentTo);
                if (edge.PerpendicularTo != null)
                {
                    for (int i = 0; i < edges.Count; i++)
                    {
                        if (edges[i].Contains(edge.PerpendicularTo)) polygonsToMove.Add(polygons[i]);
                    }
                }
            }
            if (polygonsToMove.Count == 0 && ellipsesToMove.Count == 0) MoveEdge(x, y);
            else
            {
                foreach (var polygon in polygonsToMove)
                {
                    selectedPolygon = polygon;
                    MovePolygon(x, y);
                }
                foreach (Ellipse ellipse in ellipsesToMove)
                {
                    selectedEllipse = ellipse;
                    x ??= 0;
                    y ??= 0;
                    MoveCircle(Convert.ToInt32(Canvas.GetLeft(ellipse) + ellipse.Width / 2 + x), Convert.ToInt32(Canvas.GetTop(ellipse) + ellipse.Height / 2 + y), Convert.ToInt32(ellipse.Height / 2));
                }
                selectedPolygon = null;
                selectedEllipse = null;
            }
            EdgeStackPanel.IsEnabled = false;
            EdgeRestrictionsStackPanel.IsEnabled = false;
        }

        private void btnEditEdgeOtherRestrictions_Click(object sender, RoutedEventArgs e)
        {
            Edge selectedEdge = selectedEdges[0];
            int polygonIdx = polygons.IndexOf(selectedPolygon);
            int edgeIdx = edges[polygonIdx].IndexOf(selectedEdge);
            int nextIdx = (edgeIdx + 1) % selectedPolygon.Count;
            if (radioNone.IsChecked.GetValueOrDefault())
            {
                selectedEdge.ConstLength = null;
                selectedEdge.TangentTo = null;
                if (selectedEdge.PerpendicularTo != null)
                {
                    selectedEdge.PerpendicularTo.PerpendicularTo = null;
                    selectedEdge.PerpendicularTo.Pixels.ForEach(p =>
                    {
                        canvas.Children.Remove(p);
                        p.Fill = new SolidColorBrush(Colors.Blue);
                        canvas.Children.Add(p);
                    });
                }
                selectedEdge.PerpendicularTo = null;
            }
            else if (radioLength.IsChecked.GetValueOrDefault())
            {
                int givenLength = -1;
                try
                {
                    givenLength = int.Parse(txtEdgeLength.Text);
                }
                catch (Exception)
                {

                }
                SetConstEdgeLength(selectedEdges[0], givenLength);
                selectedEdges.Clear();
            }
            else if (radioTangent.IsChecked.GetValueOrDefault())
            {
                bool stop = false;
                edges.ForEach(p => p.ForEach(e =>
                {
                    if (e.TangentTo != null && e.TangentTo == selectedEllipse)
                    {
                        MessageBox.Show("This circle is already tangent to other edge.");
                        stop = true;
                    }
                }));
                if (stop) return;
                int x = Convert.ToInt32(Canvas.GetLeft(polygons[polygonIdx][edgeIdx]));
                int y = Convert.ToInt32(Canvas.GetTop(polygons[polygonIdx][edgeIdx]));
                Canvas.SetLeft(selectedEllipse, x - selectedEllipse.Width / 2);
                Canvas.SetTop(selectedEllipse, y);
                selectedEdge.TangentTo = selectedEllipse;
                selectedEdge.Pixels.ForEach(p =>
                {
                    canvas.Children.Remove(p);
                    p.Fill = new SolidColorBrush(Colors.Orange);
                    canvas.Children.Add(p);
                });
                selectedEdge.ConstLength = null;
                selectedEdge.PerpendicularTo = null;
            }
            EdgeRestrictionsStackPanel.IsEnabled = false;
        }

        private void MoveVertex(int? x, int? y)
        {
            foreach (Rectangle rectangle in selectedPolygon)
            {
                if ((double)selectedVertex.GetValue(Canvas.LeftProperty) == (double)rectangle.GetValue(Canvas.LeftProperty)
                        && (double)selectedVertex.GetValue(Canvas.TopProperty) == (double)rectangle.GetValue(Canvas.TopProperty))
                {
                    selectedVertex = rectangle;
                }
            }
            int idx = polygons.IndexOf(selectedPolygon);
            int vertexIndex = selectedPolygon.IndexOf(selectedVertex);
            int firstIdx = vertexIndex == 0 ? selectedPolygon.Count - 1 : vertexIndex - 1;
            int secondIdx = vertexIndex == selectedPolygon.Count - 1 ? 0 : vertexIndex + 1;
            Rectangle firstNbr = selectedPolygon[firstIdx];
            Rectangle secondNbr = selectedPolygon[secondIdx];
            BresenhamLineDrawing(Convert.ToInt32(firstNbr.GetValue(Canvas.LeftProperty)), Convert.ToInt32(firstNbr.GetValue(Canvas.TopProperty)), Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.TopProperty)), true);
            BresenhamLineDrawing(Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.TopProperty)), Convert.ToInt32(secondNbr.GetValue(Canvas.LeftProperty)), Convert.ToInt32(secondNbr.GetValue(Canvas.TopProperty)), true);
            canvas.Children.Remove(polygons[idx][vertexIndex]);
            canvas.Children.Remove(selectedPolygon[vertexIndex]);
            canvas.Children.Remove(selectedVertex);
            selectedVertex.SetValue(Canvas.LeftProperty, (double)selectedVertex.GetValue(Canvas.LeftProperty) + x.GetValueOrDefault());
            selectedVertex.SetValue(Canvas.TopProperty, (double)selectedVertex.GetValue(Canvas.TopProperty) + y.GetValueOrDefault());
            edges[idx][firstIdx] = BresenhamLineDrawing(Convert.ToInt32(firstNbr.GetValue(Canvas.LeftProperty)), Convert.ToInt32(firstNbr.GetValue(Canvas.TopProperty)), Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.TopProperty)), false);
            edges[idx][vertexIndex] = BresenhamLineDrawing(Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.LeftProperty)), Convert.ToInt32(selectedPolygon[vertexIndex].GetValue(Canvas.TopProperty)), Convert.ToInt32(secondNbr.GetValue(Canvas.LeftProperty)), Convert.ToInt32(secondNbr.GetValue(Canvas.TopProperty)), false);
            selectedVertex = null;
            if (selectedEdges.Count != 2)
            {
                selectedPolygon = null;
            }

            VertexStackPanel.IsEnabled = false;
            PolygonStackPanel.IsEnabled = false;
            EdgeStackPanel.IsEnabled = false;
            EdgeRestrictionsStackPanel.IsEnabled = false;
            ClearCanvas();
        }

        private void btnEditEdgePairRestrictions_Click(object sender, RoutedEventArgs e)
        {
            if (radioPairNone.IsChecked.GetValueOrDefault())
            {
                foreach (Edge selectedEdge in selectedEdges)
                {
                    selectedEdge.ConstLength = null;
                    selectedEdge.TangentTo = null;
                    selectedEdge.PerpendicularTo = null;
                }
            }
            else if (radioPerpendicular.IsChecked.GetValueOrDefault())
            {
                Rectangle midRectangle = selectedEdges[1].Pixels[selectedEdges[1].Pixels.Count / 2];
                int polygonIdx = polygons.IndexOf(additionalSelectedPolygon);
                int otherPolygonIdx = polygons.IndexOf(selectedPolygon);
                int edgeIdx = edges[polygonIdx].IndexOf(selectedEdges[1]);
                int otherEdgeIdx = edges[otherPolygonIdx].IndexOf(selectedEdges[0]);
                int otherEdgeNextIdx = (otherEdgeIdx + 1) % selectedPolygon.Count;
                (double a, double b) = GetLineToMove();
                if (a != double.PositiveInfinity)
                {
                    int newY = Convert.ToInt32(a * Canvas.GetLeft(polygons[otherPolygonIdx][otherEdgeNextIdx]) + b);
                    int offsetY = Convert.ToInt32(newY - Canvas.GetTop(polygons[otherPolygonIdx][otherEdgeNextIdx]));
                    selectedVertex = polygons[otherPolygonIdx][otherEdgeNextIdx];
                    MoveVertex(0, offsetY);
                }
                else
                {
                    int offsetX = Convert.ToInt32(Canvas.GetLeft(polygons[otherPolygonIdx][otherEdgeIdx]) - Canvas.GetLeft(polygons[otherPolygonIdx][otherEdgeNextIdx]));
                    selectedVertex = polygons[otherPolygonIdx][otherEdgeNextIdx];
                    MoveVertex(offsetX, 0);
                }
                // set restrictions
                edges[otherPolygonIdx][otherEdgeIdx].PerpendicularTo = edges[polygonIdx][edgeIdx];
                edges[otherPolygonIdx][otherEdgeIdx].TangentTo = null;
                edges[otherPolygonIdx][otherEdgeIdx].ConstLength = null;
                edges[polygonIdx][edgeIdx].PerpendicularTo = edges[otherPolygonIdx][otherEdgeIdx];
                edges[polygonIdx][edgeIdx].TangentTo = null;
                edges[polygonIdx][edgeIdx].ConstLength = null;
                edges[otherPolygonIdx][otherEdgeIdx].Pixels.ForEach(p =>
                {
                    canvas.Children.Remove(p);
                    p.Fill = new SolidColorBrush(Colors.Aqua);
                    canvas.Children.Add(p);
                });
                edges[polygonIdx][edgeIdx].Pixels.ForEach(p =>
                {
                    canvas.Children.Remove(p);
                    p.Fill = new SolidColorBrush(Colors.Aqua);
                    canvas.Children.Add(p);
                });
            }
            EdgePairStackPanel.IsEnabled = false;
        }

        private void SetConstEdgeLength(Edge selectedEdge, int givenLength)
        {
            int selectedEdgeLength = selectedEdge.Pixels.Count;
            if (givenLength <= 0) return;
            int idx = polygons.IndexOf(selectedPolygon);
            int vertexIndex = edges[idx].IndexOf(selectedEdge);
            int nextIdx = (vertexIndex + 1) % selectedPolygon.Count;
            int firstIdx = vertexIndex == 0 ? selectedPolygon.Count - 1 : vertexIndex - 1;
            if (edges[idx][firstIdx].ConstLength != null || edges[idx][firstIdx].PerpendicularTo != null || edges[idx][firstIdx].TangentTo != null
                || edges[idx][nextIdx].ConstLength != null || edges[idx][nextIdx].PerpendicularTo != null || edges[idx][nextIdx].TangentTo != null)
            {
                MessageBox.Show("One of adjacent edges already has restrictions. Cannot change edge length.");
                return;
            }
            if (givenLength < selectedEdgeLength)
            {
                Rectangle pointToMove = selectedEdge.Pixels[givenLength];
                int offsetX = Convert.ToInt32(-Canvas.GetLeft(selectedPolygon[nextIdx]) + Canvas.GetLeft(pointToMove));
                int offsetY = Convert.ToInt32(-Canvas.GetTop(selectedPolygon[nextIdx]) + Canvas.GetTop(pointToMove));
                selectedVertex = selectedPolygon[nextIdx];
                MoveVertex(offsetX, offsetY);
                Edge edge = edges[idx][vertexIndex];
                edge.ConstLength = givenLength;
                edge.Pixels.ForEach(p =>
                {
                    canvas.Children.Remove(p);
                    p.Fill = new SolidColorBrush(Colors.Green);
                    canvas.Children.Add(p);
                });
            }
            else if (givenLength == selectedEdgeLength)
            {
                selectedEdges[0] = edges[idx][vertexIndex];
                selectedEdges[0].ConstLength = givenLength;
                selectedEdges[0].Pixels.ForEach(p =>
                {
                    canvas.Children.Remove(p);
                    p.Fill = new SolidColorBrush(Colors.Green);
                    canvas.Children.Add(p);
                });
            }
            else if (givenLength > selectedEdgeLength)
            {
                int offsetX = Convert.ToInt32(Canvas.GetLeft(polygons[idx][vertexIndex]) - Canvas.GetLeft(polygons[idx][nextIdx]));
                int offsetY = Convert.ToInt32(Canvas.GetTop(polygons[idx][vertexIndex]) - Canvas.GetTop(polygons[idx][nextIdx]) + givenLength);
                selectedVertex = selectedPolygon[nextIdx];
                MoveVertex(offsetX, offsetY);
                selectedEdges[0] = edges[idx][vertexIndex];
                selectedEdges[0].ConstLength = givenLength;
                selectedEdges[0].Pixels.ForEach(p =>
                {
                    canvas.Children.Remove(p);
                    p.Fill = new SolidColorBrush(Colors.Green);
                    canvas.Children.Add(p);
                });
            }
            selectedEdges[0].TangentTo = null;
            selectedEdges[0].PerpendicularTo = null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<Rectangle> poly1 = new();
            List<Edge> drawnEdges = new();
            List<Rectangle> poly2 = new();
            poly1.Add(CreateVertex(100, 100));
            poly1.Add(CreateVertex(100, 300));
            poly1.Add(CreateVertex(300, 300));
            poly1.Add(CreateVertex(300, 100));
            for (int i = 0; i < poly1.Count - 1; i++)
            {
                drawnEdges.Add(BresenhamLineDrawing(Convert.ToInt32(poly1[i].GetValue(Canvas.LeftProperty)), Convert.ToInt32(poly1[i].GetValue(Canvas.TopProperty)), Convert.ToInt32(poly1[i + 1].GetValue(Canvas.LeftProperty)), Convert.ToInt32(poly1[i + 1].GetValue(Canvas.TopProperty)), false));
            }
            drawnEdges.Add(BresenhamLineDrawing(Convert.ToInt32(poly1[^1].GetValue(Canvas.LeftProperty)), Convert.ToInt32(poly1[^1].GetValue(Canvas.TopProperty)), Convert.ToInt32(poly1[0].GetValue(Canvas.LeftProperty)), Convert.ToInt32(poly1[0].GetValue(Canvas.TopProperty)), false));
            polygons.Add(poly1);
            edges.Add(drawnEdges);
            drawnEdges = new();
            selectedPolygon = polygons[0];
            selectedEdges.Add(edges[0][0]);
            SetConstEdgeLength(selectedEdges[0], 200);
            selectedEdges.Clear();
            selectedEdges.Add(edges[0][2]);
            SetConstEdgeLength(edges[0][2], 200);
            poly2.Add(CreateVertex(600, 200));
            poly2.Add(CreateVertex(700, 300));
            poly2.Add(CreateVertex(600, 400));
            poly2.Add(CreateVertex(500, 400));
            poly2.Add(CreateVertex(400, 300));
            poly2.Add(CreateVertex(500, 200));
            for (int i = 0; i < poly2.Count - 1; i++)
            {
                drawnEdges.Add(BresenhamLineDrawing(Convert.ToInt32(poly2[i].GetValue(Canvas.LeftProperty)), Convert.ToInt32(poly2[i].GetValue(Canvas.TopProperty)), Convert.ToInt32(poly2[i + 1].GetValue(Canvas.LeftProperty)), Convert.ToInt32(poly2[i + 1].GetValue(Canvas.TopProperty)), false));
            }
            drawnEdges.Add(BresenhamLineDrawing(Convert.ToInt32(poly2[^1].GetValue(Canvas.LeftProperty)), Convert.ToInt32(poly2[^1].GetValue(Canvas.TopProperty)), Convert.ToInt32(poly2[0].GetValue(Canvas.LeftProperty)), Convert.ToInt32(poly2[0].GetValue(Canvas.TopProperty)), false));
            polygons.Add(poly2);
            edges.Add(drawnEdges);
            Ellipse ellipse1 = createEllipse(500, 500, 100);
            ellipses.Add(ellipse1);
            canvas.Children.Add(ellipse1);
            selectedEllipse = ellipse1;
            selectedPolygon = polygons[1];
            selectedEdges.Clear();
            selectedEdges.Add(edges[1][2]);
            SetTangentRelation(edges[1][2]);
            Ellipse ellipse2 = createEllipse(700, 100, 50);
            ellipses.Add(ellipse2);
            canvas.Children.Add(ellipse2);
            selectedPolygon = null;
            selectedEdges.Clear();
        }

        private Ellipse createEllipse(int? x, int? y, int? r)
        {
            Ellipse ellipse = new();
            ellipse.Width = (double)r * 2;
            ellipse.Height = (double)r * 2;
            ellipse.MouseLeftButtonDown += Ellipse_MouseLeftButtonDown;
            ellipse.Fill = new SolidColorBrush(Colors.Orange);
            ellipse.SetValue(Canvas.TopProperty, (double)y - r);
            ellipse.SetValue(Canvas.LeftProperty, (double)x - r);
            return ellipse;
        }

        private void SetTangentRelation(Edge selectedEdge)
        {
            int polygonIdx = polygons.IndexOf(selectedPolygon);
            int edgeIdx = edges[polygonIdx].IndexOf(selectedEdge);
            bool stop = false;
            edges.ForEach(p => p.ForEach(e =>
            {
                if (e.TangentTo != null && e.TangentTo == selectedEllipse)
                {
                    MessageBox.Show("This circle is already tangent to other edge.");
                    stop = true;
                }
            }));
            if (stop) return;
            int x = Convert.ToInt32(Canvas.GetLeft(polygons[polygonIdx][edgeIdx]));
            int y = Convert.ToInt32(Canvas.GetTop(polygons[polygonIdx][edgeIdx]));
            Canvas.SetLeft(selectedEllipse, x - selectedEllipse.Width / 2);
            Canvas.SetTop(selectedEllipse, y);
            selectedEdge.TangentTo = selectedEllipse;
            selectedEdge.Pixels.ForEach(p =>
            {
                canvas.Children.Remove(p);
                p.Fill = new SolidColorBrush(Colors.Orange);
                canvas.Children.Add(p);
            });
            selectedEdge.ConstLength = null;
            selectedEdge.PerpendicularTo = null;
        }

        private (double, double) GetLineToMove()
        {
            int polygonIdx = polygons.IndexOf(additionalSelectedPolygon);
            int otherPolygonIdx = polygons.IndexOf(selectedPolygon);
            int edgeIdx = edges[polygonIdx].IndexOf(selectedEdges[1]);
            int otherEdgeIdx = edges[otherPolygonIdx].IndexOf(selectedEdges[0]);
            int nextIdx = (edgeIdx + 1) % additionalSelectedPolygon.Count;
            Rectangle midRectangle = polygons[otherPolygonIdx][otherEdgeIdx];
            int xDiff = Convert.ToInt32(-(Canvas.GetLeft(polygons[polygonIdx][nextIdx]) - Canvas.GetLeft(polygons[polygonIdx][edgeIdx])));
            int yDiff = Convert.ToInt32(-(Canvas.GetTop(polygons[polygonIdx][nextIdx]) - Canvas.GetTop(polygons[polygonIdx][edgeIdx])));
            int midX = Convert.ToInt32(Canvas.GetLeft(midRectangle));
            int midY = Convert.ToInt32(Canvas.GetTop(midRectangle));
            if (yDiff == 0) return (double.PositiveInfinity, double.PositiveInfinity);
            double a = (double)-xDiff / (double)yDiff;
            double b = midY - a * midX;
            return (a, b);
        }

        private void MovePolygon(int? polygonOffsetX, int? polygonOffsetY)
        {
            int idx = polygons.IndexOf(selectedPolygon);
            for (int i = 0; i < selectedPolygon.Count - 1; i++)
            {
                int? constLength = edges[idx][i].ConstLength;
                Ellipse edgeTangentTo = edges[idx][i].TangentTo;
                var edgePerpendicularTo = edges[idx][i].PerpendicularTo;
                canvas.Children.Remove(polygons[idx][i]);
                canvas.Children.Remove(selectedPolygon[i]);
                BresenhamLineDrawing(Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.TopProperty)), Convert.ToInt32(selectedPolygon[i + 1].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[i + 1].GetValue(Canvas.TopProperty)), true);
                int newX = (polygonOffsetX.HasValue) ? Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.LeftProperty)) + polygonOffsetX.Value : Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.LeftProperty));
                int newY = (polygonOffsetY.HasValue) ? Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.TopProperty)) + polygonOffsetY.Value : Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.TopProperty));
                int newX1 = (polygonOffsetX.HasValue) ? Convert.ToInt32((double)selectedPolygon[i + 1].GetValue(Canvas.LeftProperty)) + polygonOffsetX.Value : Convert.ToInt32((double)selectedPolygon[i + 1].GetValue(Canvas.LeftProperty));
                int newY1 = (polygonOffsetY.HasValue) ? Convert.ToInt32((double)selectedPolygon[i + 1].GetValue(Canvas.TopProperty)) + polygonOffsetY.Value : Convert.ToInt32((double)selectedPolygon[i + 1].GetValue(Canvas.TopProperty));
                edges[idx][i] = BresenhamLineDrawing(newX, newY, newX1, newY1, false);
                edges[idx][i].ConstLength = constLength;
                edges[idx][i].TangentTo = edgeTangentTo;
                edges[idx][i].PerpendicularTo = edgePerpendicularTo;
            }
            int? constLengthLast = edges[idx][^1].ConstLength;
            Ellipse edgeTangentToLast = edges[idx][^1].TangentTo;
            var edgePerpendicularToLast = edges[idx][^1].PerpendicularTo;
            BresenhamLineDrawing(Convert.ToInt32((double)selectedPolygon[^1].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[^1].GetValue(Canvas.TopProperty)), Convert.ToInt32((double)selectedPolygon[0].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[0].GetValue(Canvas.TopProperty)), true);
            canvas.Children.Remove(polygons[idx][^1]);
            canvas.Children.Remove(selectedPolygon[^1]);
            int newXLast = (polygonOffsetX.HasValue) ? Convert.ToInt32((double)selectedPolygon[^1].GetValue(Canvas.LeftProperty) + polygonOffsetX.Value) : Convert.ToInt32((double)selectedPolygon[^1].GetValue(Canvas.LeftProperty));
            int newYLast = (polygonOffsetY.HasValue) ? Convert.ToInt32((double)selectedPolygon[^1].GetValue(Canvas.TopProperty) + polygonOffsetY.Value) : Convert.ToInt32((double)selectedPolygon[^1].GetValue(Canvas.TopProperty));
            int newX0 = (polygonOffsetX.HasValue) ? Convert.ToInt32((double)selectedPolygon[0].GetValue(Canvas.LeftProperty) + polygonOffsetX.Value) : Convert.ToInt32((double)selectedPolygon[0].GetValue(Canvas.LeftProperty));
            int newY0 = (polygonOffsetY.HasValue) ? Convert.ToInt32((double)selectedPolygon[0].GetValue(Canvas.TopProperty) + polygonOffsetY.Value) : Convert.ToInt32((double)selectedPolygon[0].GetValue(Canvas.TopProperty));
            edges[idx][^1] = BresenhamLineDrawing(newXLast, newYLast, newX0, newY0, false);
            edges[idx][^1].ConstLength = constLengthLast;
            edges[idx][^1].TangentTo = edgeTangentToLast;
            edges[idx][^1].PerpendicularTo = edgePerpendicularToLast;
            for (int i = 0; i < selectedPolygon.Count; i++)
            {
                if (polygonOffsetX.HasValue) selectedPolygon[i] = CreateVertex(Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.LeftProperty) + polygonOffsetX.Value), Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.TopProperty)));
                if (polygonOffsetY.HasValue) selectedPolygon[i] = CreateVertex(Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[i].GetValue(Canvas.TopProperty) + polygonOffsetY.Value));
            }
            edges.ForEach(p => p.ForEach(e =>
            {
                if (e.ConstLength.HasValue)
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Green));
                }
                else if (e.TangentTo != null)
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Orange));
                }
                else if (e.PerpendicularTo != null)
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Aqua));
                }
                else
                {
                    e.Pixels.ForEach(x => x.Fill = new SolidColorBrush(Colors.Blue));
                }

            }));
            VertexStackPanel.IsEnabled = false;
            PolygonStackPanel.IsEnabled = false;
            ClearCanvas();
        }

        private void MoveCircle(int? x, int? y, int? r)
        {
            if (x.HasValue) selectedEllipse.SetValue(Canvas.LeftProperty, (double)(x - selectedEllipse.Width / 2));
            if (y.HasValue) selectedEllipse.SetValue(Canvas.TopProperty, (double)(y - selectedEllipse.Width / 2));
            if (r.HasValue)
            {
                Canvas.SetLeft(selectedEllipse, Canvas.GetLeft(selectedEllipse) + selectedEllipse.Width / 2 - r.Value);
                Canvas.SetTop(selectedEllipse, Canvas.GetTop(selectedEllipse) + selectedEllipse.Height / 2 - r.Value);
                selectedEllipse.Width = (double)r * 2;
                selectedEllipse.Height = (double)r * 2;
            }
        }

        private void MoveEdge(int? x, int? y)
        {
            int polygonIdx = polygons.IndexOf(selectedPolygon);
            int edgeIdx = edges[polygonIdx].IndexOf(selectedEdges[0]);
            int? constLength = selectedEdges[0].ConstLength;
            Ellipse edgeTangentTo = selectedEdges[0].TangentTo;
            var edgePerpendicularTo = selectedEdges[0].PerpendicularTo;
            int edgeIdx1 = (edgeIdx + 1) % selectedPolygon.Count;
            int firstIdx = edgeIdx == 0 ? selectedPolygon.Count - 1 : edgeIdx - 1;
            int secondIdx = (edgeIdx + 2) % selectedPolygon.Count;

            int newX1 = Convert.ToInt32(Canvas.GetLeft(selectedPolygon[edgeIdx]) + x.GetValueOrDefault());
            int newX2 = Convert.ToInt32(Canvas.GetLeft(selectedPolygon[(edgeIdx + 1) % selectedPolygon.Count]) + x.GetValueOrDefault());
            int newY1 = Convert.ToInt32(Canvas.GetTop(selectedPolygon[edgeIdx]) + y.GetValueOrDefault());
            int newY2 = Convert.ToInt32(Canvas.GetTop(selectedPolygon[(edgeIdx + 1) % selectedPolygon.Count]) + y.GetValueOrDefault());
            canvas.Children.Remove(polygons[polygonIdx][firstIdx]);
            canvas.Children.Remove(polygons[polygonIdx][edgeIdx]);
            canvas.Children.Remove(polygons[polygonIdx][edgeIdx1]);
            canvas.Children.Remove(polygons[polygonIdx][secondIdx]);
            BresenhamLineDrawing(Convert.ToInt32((double)selectedPolygon[firstIdx].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[firstIdx].GetValue(Canvas.TopProperty)), Convert.ToInt32(selectedPolygon[edgeIdx].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[edgeIdx].GetValue(Canvas.TopProperty)), true);
            edges[polygonIdx][firstIdx] = BresenhamLineDrawing(Convert.ToInt32((double)selectedPolygon[firstIdx].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[firstIdx].GetValue(Canvas.TopProperty)), newX1, newY1, false);
            BresenhamLineDrawing(Convert.ToInt32((double)selectedPolygon[edgeIdx].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[edgeIdx].GetValue(Canvas.TopProperty)), Convert.ToInt32(selectedPolygon[edgeIdx1].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[edgeIdx1].GetValue(Canvas.TopProperty)), true);
            edges[polygonIdx][edgeIdx] = BresenhamLineDrawing(newX1, newY1, newX2, newY2, false);
            BresenhamLineDrawing(Convert.ToInt32((double)selectedPolygon[edgeIdx1].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[edgeIdx1].GetValue(Canvas.TopProperty)), Convert.ToInt32(selectedPolygon[secondIdx].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[secondIdx].GetValue(Canvas.TopProperty)), true);
            edges[polygonIdx][edgeIdx1] = BresenhamLineDrawing(newX2, newY2, Convert.ToInt32((double)selectedPolygon[secondIdx].GetValue(Canvas.LeftProperty)), Convert.ToInt32((double)selectedPolygon[secondIdx].GetValue(Canvas.TopProperty)), false);
            Canvas.SetLeft(selectedPolygon[edgeIdx], newX1);
            Canvas.SetTop(selectedPolygon[edgeIdx], newY1);
            Canvas.SetLeft(selectedPolygon[edgeIdx1], newX2);
            Canvas.SetTop(selectedPolygon[edgeIdx1], newY2);
            if (constLength.HasValue)
            {
                edges[polygonIdx][edgeIdx].Pixels.ForEach(x =>
                {
                    canvas.Children.Remove(x);
                    x.Fill = new SolidColorBrush(Colors.Green);
                    canvas.Children.Add(x);
                });
            }
            else if (edgeTangentTo != null)
            {
                edges[polygonIdx][edgeIdx].Pixels.ForEach(x =>
                {
                    canvas.Children.Remove(x);
                    x.Fill = new SolidColorBrush(Colors.Orange);
                    canvas.Children.Add(x);
                });
            }
            else if (edgePerpendicularTo != null)
            {
                edges[polygonIdx][edgeIdx].Pixels.ForEach(x =>
                {
                    canvas.Children.Remove(x);
                    x.Fill = new SolidColorBrush(Colors.Aqua);
                    canvas.Children.Add(x);
                });
            }
            selectedEdges.Clear();
            EdgeStackPanel.IsEnabled = false;
            EdgeRestrictionsStackPanel.IsEnabled = false;
            ClearCanvas();
        }

        private void ClearCanvas()
        {
            for(int i=0;i<canvas.Children.Count;i++)
            {
                if(canvas.Children[i] is Rectangle)
                {
                    Rectangle rectangle = canvas.Children[i] as Rectangle;
                    if(rectangle.Width == 10 && rectangle.Height == 10)
                    { 
                        bool found = false;
                        foreach (var polygon in polygons)
                        {
                            foreach (Rectangle rectangle1 in polygon)
                            {
                                if ((double)rectangle1.GetValue(Canvas.LeftProperty) == (double)rectangle.GetValue(Canvas.LeftProperty)
                                    && (double)rectangle1.GetValue(Canvas.TopProperty) == (double)rectangle.GetValue(Canvas.TopProperty))
                                {
                                    found = true;
                                }
                            }
                        }
                        if (!found) canvas.Children.RemoveAt(i);
                    }
                }
            }
        }
    }
}
