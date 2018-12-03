using System;
using System.Collections.Generic;

namespace FontReader.Read
{
    public class Glyph {
        /// <summary>
        /// Glyph type. Used to interpret the other parts of a glyph structure
        /// </summary>
        public GlyphTypes GlyphType;

        /// <summary>
        /// Number of connected point sets in the glyph
        /// </summary>
        public int NumberOfContours;

        // Container box size
        public double xMin, xMax, yMin, yMax;

        /// <summary>
        /// Components if this is a compound glyph (made of transformed copies of other glyphs)
        /// Null if this is a simple glyph
        /// </summary>
        public CompoundComponent[] Components;

        public GlyphPoint[] Points;
        public int[] ContourEnds;

        /// <summary>
        /// Reduce the glyph to a set of simple point contours.
        /// Curves will be re-drawn as segments
        /// </summary>
        public List<GlyphPoint[]> NormalisedContours(double xScale, double yScale, double xOffset, double yOffset) {
            if (Points == null || Points.Length < 1) return null;
            if (ContourEnds == null || ContourEnds.Length < 1) return null;

            var outp = new List<GlyphPoint[]>();
            var p = 0;
            var c = 0;
            var contour = new List<GlyphPoint>();
            var plen = Points.Length;
            var hasCurves = false;

            while (p < plen)
            {
                var point = Points[p];

                if (point.OnCurve) { hasCurves = true; }

                var xpos = (point.X - xMin) * xScale;
                var ypos = (point.Y - yMin) * yScale;

                if (xpos < 0) xpos = 0;
                if (ypos < 0) ypos = 0;

                contour.Add(new GlyphPoint { X = xpos + xOffset, Y = ypos + yOffset, OnCurve = point.OnCurve });

                if (p == ContourEnds[c])
                {
                    if (hasCurves) {
                        outp.Add(NormaliseContour(contour));
                    } else {
                        outp.Add(contour.ToArray());
                    }
                    contour.Clear();
                    c++;
                }

                p++;
            }
            return outp;
        }

        /// <summary>
        /// Return the boundaries of the points on this glyph.
        /// This ignored the stated min/max bounds for positioning
        /// </summary>
        public void GetPointBounds(out double xmin, out double xmax, out double ymin, out double ymax)
        {
            xmin = 0d;
            xmax = 4d;
            ymin = 0d;
            ymax = 4d;
            if (Points == null) return;
            for (int i = 0; i < Points.Length; i++)
            {
                var p = Points[i];
                xmin = Math.Min(p.X, xmin);
                xmax = Math.Max(p.X, xmax);
                ymin = Math.Min(p.Y, ymin);
                ymax = Math.Max(p.Y, ymax);
            }
        }

        /// <summary>
        /// break curves into segments where needed
        /// </summary>
        private GlyphPoint[] NormaliseContour(IReadOnlyList<GlyphPoint> contour)
        {
            var final = new List<GlyphPoint>();
            var offCurve = new List<GlyphPoint>();
            var len = contour.Count;

            for (int i = 0; i <= len; i++)
            {
                var mid = contour[i % len];
                if (mid.OnCurve) {
                    if (offCurve.Count < 1) {
                        final.Add(mid); // nothing special
                        offCurve.Clear();
                        offCurve.Add(mid);
                    } else { // interpolation needed
                        offCurve.Add(mid);
                        final.AddRange(InterpolateCurve_Complex(offCurve));
                        offCurve.Clear();
                        offCurve.Add(mid);
                    }
                    continue;
                }

                // add off-curve points until we hit another on-curve.
                offCurve.Add(mid);

                // TODO: This is resulting in distorted curves.
                // I think the font spec must be doing something different.
                // I wonder if it's making 'fake' on-curve points between
                // off-curve points -- so all curves are order-2, but some
                // of the on-curve points aren't specified.
            }

            return final.ToArray();
        }

        /// <summary>
        /// A more refined curve breaker for larger sizes
        /// </summary>
        private static IEnumerable<GlyphPoint> InterpolateCurve_Complex(List<GlyphPoint> points)
        {
            /*var dx1 = prev.X - mid.X;
            var dy1 = prev.Y - mid.Y;
            var dx2 = mid.X - next.X;
            var dy2 = mid.Y - next.Y;
            var dist = Math.Sqrt((dx1 * dx1) + (dy1 * dy1) + (dx2 * dx2) + (dy2 * dy2));
            if (dist <= 1) {
                yield return mid;
                yield break;
            }*/

            var inv = 0.01;//1.0 / dist;
            var pp = points[0];
            var minStep = 1;   // larger = less refined curve

            for (double t = 0; t < 1; t+= inv)
            {
                var pt = ReducePoints_Rec(points.ToArray(), t, 1.0 - t);

                if (Math.Abs(pp.X - pt.X) > minStep || Math.Abs(pp.Y - pt.Y) > minStep) {
                    pp = pt;
                    yield return pt;
                }
            }
        }

        /// <summary>
        /// Naïve de Casteljau's algorithm
        /// TODO: make this in-place and iterative and merge back into parent
        /// </summary>
        private static GlyphPoint ReducePoints_Rec(GlyphPoint[] points, double t, double it)
        {
            if (points.Length == 1) return points[0];

            var next = new GlyphPoint[points.Length - 1];
            for (int i = 0; i < next.Length; i++)
            {
                var x = it * points[i].X + t * points[i+1].X;
                var y = it * points[i].Y + t * points[i+1].Y;
                next[i] = new GlyphPoint { X = x, Y = y };
            }
            return ReducePoints_Rec(next, t, it);
        }
    }
}