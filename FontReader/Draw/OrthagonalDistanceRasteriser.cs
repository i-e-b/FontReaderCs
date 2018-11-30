using System.Collections.Generic;
using FontReader.Read;

namespace FontReader.Draw
{
    /// <summary>
    /// Experimental rasteriser that makes a pair of distance fields.
    /// Inside of the character is where both fields are 0 or negative
    /// </summary>
    /// <remarks>
    /// The idea is to cache the distance fields and interpolate them to render multiple sizes,
    /// so we're allowed to spend a *lot* more time making a good distance field.
    /// </remarks>
    public class OrthagonalDistanceRasteriser
    {
        /// <summary>
        /// Render a glyph at the given scale. Result is a pair of signed distance fields
        /// Each orthagonal to the other
        /// </summary>
        public static void Render(Glyph glyph, float xScale, float yScale, out float baseline, out sbyte[,] horzSdf, out sbyte[,] vertSdf)
        {
            baseline = 0;
            horzSdf = null;
            vertSdf = null;
            if (glyph == null) return;
            if (glyph.GlyphType != GlyphTypes.Simple) return;

            glyph.GetPointBounds(out var xmin, out var xmax, out var ymin, out var ymax);
            baseline = (float) (glyph.yMin * yScale);

            var width = (int)((xmax - xmin) * xScale) + 2;
            var height = (int)((ymax - ymin) * yScale) + 2;

            horzSdf = new sbyte[height, width];
            vertSdf = new sbyte[height, width];

            var pairs = SplitContours(glyph, xScale, yScale);

            ScanHorz(horzSdf, pairs);
        }

        private static void ScanHorz(sbyte[,] field, List<PointPair> pairs)
        {
            var ymax = field.GetLength(0);
            var xmax = field.GetLength(1);

            for (int y = 0; y < ymax; y++)
            {
                var wind = 0;
                var crossidx = 0;
                var crossings = SortedCrossingsY(y, pairs);
                if ((crossings?.Count ?? 0) < 1) continue; // blank line

                for (int x = 0; x < xmax; x++)
                {
                    // scan forward through crossings, adding them to the winding.
                }
            }
        }

        /// <summary>
        /// An ordered list of x positions where lines cross the Y axis
        /// </summary>
        private static List<Crossing> SortedCrossingsY(double y, List<PointPair> pairs)
        {
            var outp = new List<Crossing>();
            foreach (var pair in pairs)
            {
                var xing = new Crossing();
                if (pair.y0 > y && pair.y1 < y) // counter-clockwise
                {
                    xing.clockwise = false;
                }
                else if (pair.y0 < y && pair.y1 > y) // clockwise
                {
                    xing.clockwise = true;
                }
                else continue; // else not crossing


                // TODO: work out where the lines intersect, set xing
            }
            outp.Sort((a,b)=> a.position.CompareTo(b.position));
            return outp;
        }

        // split every connected pair of points into a list
        private static List<PointPair> SplitContours(Glyph glyph, float xScale, float yScale)
        {
            var contours = glyph.NormalisedContours(xScale, yScale, 0, 0);
            var outp = new List<PointPair>();

            foreach (var contour in contours) {
                var len = contour.Length;
                var offs = len - 1;
                for (int i = 0; i < len; i++)
                {
                    var curr = contour[ i              ];
                    var prev = contour[(i + offs) % len];
                    var next = contour[(i + 1)    % len];

                    outp.Add(new PointPair {
                        x0 = prev.X, y0 = prev.Y,
                        x1 = curr.X, y1 = curr.Y
                    });
                    outp.Add(new PointPair {
                        x0 = curr.X, y0 = curr.Y,
                        x1 = next.X, y1 = next.Y
                    });
                }
            }

            return outp;
        }

        /// <summary>
        /// Represents a directional line segment
        /// </summary>
        internal struct PointPair
        {
            public double x0, y0, x1, y1;
        }

        private struct Crossing
        {
            public double position;
            public bool clockwise;
        }
    }
}