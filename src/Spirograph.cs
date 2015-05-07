using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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

      public static List<Line> AsLines(double ring_radius, double gear_radius, double pen_offset = 0.0, bool epicycloid = false, double steps = 10000, double startParam = 0.0, double endParam = 100.0)
      {
         List<Line> output = new List<Line>();
         double delta = (endParam - startParam) / steps;
         for (double t = startParam; t < endParam; t += delta) {
   	      output.Add(computeLine(ring_radius, gear_radius, pen_offset, t, t + delta, epicycloid));
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

      public static string WritePolygonsToSVG(List<Polygon> polygons, string filePath)
      {
         using (var sw = new StreamWriter(filePath, false))
         {
            sw.Write("<?xml version='1.0' standalone='no'?>\n<svg version='1.1' xmlns='http://www.w3.org/2000/svg'>\n");
            foreach (Polygon p in polygons)
            {
               sw.Write("<polygon points='");
               foreach (Point pt in p.Points)
               {
                  sw.Write(string.Format("{0} {1} ", pt.X, pt.Y));
               }
               sw.Write("'/>\n");
            }
            sw.Write("</svg>");
            sw.Flush();
         }

         return filePath;
      }

      public static string WriteArcsToSvg(List<Arc> arcs, double strokeWidth, string filePath)
      {
         using (var sw = new StreamWriter(filePath, false))
         {
            sw.Write("<?xml version='1.0' standalone='no'?>\n<svg version='1.1' xmlns='http://www.w3.org/2000/svg'>\n");
            foreach (Arc a in arcs)
            {
               sw.Write("<path d='M{0},{1} A{2},{3} 0 0,1 {4},{5}' style='stroke: #000000; stroke-width:{6}; fill:none'/>\n", a.StartPoint.X, a.StartPoint.Y, a.Radius, a.Radius, a.EndPoint.X, a.EndPoint.Y, strokeWidth);
            }
            sw.Write("</svg>");
            sw.Flush();
         }

         return filePath;
      }
   }
}
