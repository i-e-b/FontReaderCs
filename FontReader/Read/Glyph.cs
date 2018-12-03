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

        public char SourceCharacter;
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

            while (p < plen)
            {
                var point = Points[p];

                var xpos = (point.X - xMin) * xScale;
                var ypos = (point.Y - yMin) * yScale;

                if (xpos < 0) xpos = 0;
                if (ypos < 0) ypos = 0;

                contour.Add(new GlyphPoint { X = xpos + xOffset, Y = ypos + yOffset, OnCurve = point.OnCurve });

                if (p == ContourEnds[c])
                {
                    // TODO: merge this up to avoid a double-copy
                    outp.Add(NormaliseContour(contour));
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
            var len = contour.Count;
            var final = new List<GlyphPoint>(len * 4);
            var offs = len - 1;

            // If we get more than one 'off-curve' point in a row,
            // then we add a new virtual point between the two off-curve
            // points, and interpolate a bezier curve with 1 control point
            for (int i = 0; i <= len; i++)
            {
                var current = contour[i % len];
                var prev = contour[(i+offs) % len];
                var next = contour[(i+1   ) % len];

                // if current is on-curve, just add it.
                // if current is off-curve, but next is on-curve, do a simple interpolate
                // if current AND next are off-curve, create a virtual point and interpolate

                if (current.OnCurve) // simple corner point
                {
                    final.Add(current);
                }
                else if (next.OnCurve && prev.OnCurve) // simple curve
                {
                    final.AddRange(InterpolateCurve(prev, current, next));
                }
                else if (prev.OnCurve) // single virtual curve forward
                {
                    var virt = new GlyphPoint
                    {
                        X = (current.X + next.X) / 2.0,
                        Y = (current.Y + next.Y) / 2.0
                    };
                    final.AddRange(InterpolateCurve(prev, current, virt));
                }
                else if (next.OnCurve) // single virtual curve behind
                {
                    var virt = new GlyphPoint
                    {
                        X = (current.X + prev.X) / 2.0,
                        Y = (current.Y + prev.Y) / 2.0
                    };
                    final.AddRange(InterpolateCurve(virt, current, next));
                }
                else // double virtual curve
                {
                    var virtPrev = new GlyphPoint
                    {
                        X = (current.X + prev.X) / 2.0,
                        Y = (current.Y + prev.Y) / 2.0
                    };
                    var virtNext = new GlyphPoint
                    {
                        X = (current.X + next.X) / 2.0,
                        Y = (current.Y + next.Y) / 2.0
                    };
                    final.AddRange(InterpolateCurve(virtPrev, current, virtNext));
                }
            }

            return final.ToArray();
        }

        /// <summary>
        /// A more refined curve breaker for larger sizes
        /// </summary>
        private static IEnumerable<GlyphPoint> InterpolateCurve(GlyphPoint start, GlyphPoint ctrl, GlyphPoint end)
        {
            // Estimate a step size
            var dx1 = start.X - ctrl.X;
            var dy1 = start.Y - ctrl.Y;
            var dx2 = ctrl.X - end.X;
            var dy2 = ctrl.Y - end.Y;
            var dist = Math.Sqrt((dx1 * dx1) + (dy1 * dy1) + (dx2 * dx2) + (dy2 * dy2));
            if (dist <= 1) {
                yield return start;
                yield break;
            }

            var minStep = 1.0d;   // larger = less refined curve, but faster
            var inv = minStep / dist; // estimated step size. Refined by 'minStep' checks in the main loop
            var pp = start;

            for (double t = 0; t < 1; t+= inv)
            {
                var pt = InterpolatePoints(start, ctrl, end, t, 1.0 - t);

                if (Math.Abs(pp.X - pt.X) > minStep || Math.Abs(pp.Y - pt.Y) > minStep) {
                    pp = pt;
                    yield return pt;
                }
            }
        }

        /// <summary>
        /// de Casteljau's algorithm for exactly 3 points
        /// </summary>
        private static GlyphPoint InterpolatePoints(GlyphPoint start, GlyphPoint ctrl, GlyphPoint end, double t, double it)
        {
            var aX = it * start.X + t * ctrl.X;
            var aY = it * start.Y + t * ctrl.Y;
            
            var bX = it * ctrl.X + t * end.X;
            var bY = it * ctrl.Y + t * end.Y;

            return new GlyphPoint{
                X = it * aX + t * bX,
                Y = it * aY + t * bY
            };
        }
    }
}