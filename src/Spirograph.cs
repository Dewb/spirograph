using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Geometry;

///////////////////////////////////////////////////////////////////
/// NOTE: This project requires references to the ProtoInterface
/// and ProtoGeometry DLLs. These are found in the Dynamo install
/// directory.
///////////////////////////////////////////////////////////////////

namespace Spirograph
{
   public class Spirograph
   {
      private static Point computePoint(double R, double r, double p, double t, bool epicycloid)
      {
         double e = epicycloid ? -1 : 1;
         double a = R - e * r;
         double b = r / R * t;
         double c = (1 - e * r / R) * t;
         double x = a * Math.Cos(b) - p * Math.Cos(c);
         double y = a * Math.Sin(b) + p * Math.Sin(c);
         return Point.ByCoordinates(x, y);
      }

      private static Line computeLine(double R, double r, double p, double t1, double t2, bool epicycloid)
      {
         return Line.ByStartPointEndPoint(computePoint(R, r, p, t1, epicycloid), computePoint(R, r, p, t2, epicycloid));
      }

      public static List<Line> AsLines(double ring_radius, double gear_radius, double pen_offset = 0.0, double step_size = 1.0, bool epicycloid = false)
      {
         List<Line> output = new List<Line>();
         for (double t = -0.38 * Math.PI; t < 41.47 * Math.PI; t += step_size) {
   	      output.Add(computeLine(ring_radius, gear_radius, pen_offset, t, t + step_size, epicycloid));
         }
         return output;
      }

      private static Tuple<Point, Point> getStrokeCornersForPoint(Point pt, Vector normal, double startWidth, double endWidth, double minRadius, double maxRadius)
      {
         double d = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
         double width = (d - minRadius) / (maxRadius - minRadius) * (endWidth - startWidth) + startWidth;
         using (Vector v = normal.Cross(Vector.ByCoordinates(0, 0, 1)).Normalized().Scale(width))
         {
            return new Tuple<Point, Point>(pt.Add(v), pt.Subtract(v));
         }
      }

      public static List<Polygon> StrokeLinesWithPolygons(List<Line> lines, double startWidth, double endWidth = -1, double minRadius = -1, double maxRadius = -1)
      {
         List<Polygon> output = new List<Polygon>();
         if (endWidth < 0)
         {
            endWidth = startWidth;
         }
         if (minRadius < 0 || maxRadius < 0)
         {
            minRadius = maxRadius = 1;
         }

         for (int i = 0; i < lines.Count; i++)
         {
            Line line = lines[i];
            Line nextLine = lines[i < lines.Count - 1 ? i + 1 : lines.Count - 1];
            Line prevLine = lines[i > 0 ? i - 1 : 0];
            Vector n1 = Vector.ByTwoPoints(prevLine.StartPoint, line.EndPoint);
            Vector n2 = Vector.ByTwoPoints(line.StartPoint, nextLine.EndPoint);
            Tuple<Point, Point> corner1 = getStrokeCornersForPoint(line.StartPoint, n1, startWidth, endWidth, minRadius, maxRadius);
            Tuple<Point, Point> corner2 = getStrokeCornersForPoint(line.EndPoint, n2, startWidth, endWidth, minRadius, maxRadius);
            List<Point> polyPoints = new List<Point> { corner1.Item1, corner1.Item2, corner2.Item2, corner2.Item1 };
            output.Add(Polygon.ByPoints(polyPoints));

            n1.Dispose();
            n2.Dispose();
            foreach (Point p in polyPoints)
            {
               p.Dispose();
            }
         }
         return output;
      }
   }
}
