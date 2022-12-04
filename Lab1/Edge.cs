using System.Collections.Generic;
using System.Windows.Shapes;

namespace Lab1
{
    class Edge
    {
        public List<Rectangle> Pixels { get; set; }
        public int? ConstLength { get; set; }
        public Ellipse TangentTo { get; set; }
        public Edge PerpendicularTo { get; set; }
        public Edge()
        {
            Pixels = new();
        }
    }
}
