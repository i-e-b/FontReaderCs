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
        public List<GlyphPoint[]> NormalisedContours(double xScale, double yScale) {
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

                if (point != null)
                {
                    if (point.OnCurve) { hasCurves = true; }

                    var xpos = (point.X - xMin) * xScale;
                    var ypos = (point.Y - yMin) * yScale;

                    if (xpos < 0) xpos = 0;
                    if (ypos < 0) ypos = 0;

                    contour.Add(new GlyphPoint { X = xpos, Y = ypos, OnCurve = point.OnCurve });
                }

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
        /// break curves into segments where needed
        /// </summary>
        private GlyphPoint[] NormaliseContour(List<GlyphPoint> contour)
        {
            var final = new List<GlyphPoint>();
            var len = contour.Count;
            var offs = len - 1;

            for (int i = 0; i < len; i++)
            {
                var mid = contour[i];
                if (mid.OnCurve) { // normal point
                    final.Add(mid);
                    continue;
                }

                // curve point
                var prev = contour[(i+offs) % len];
                var next = contour[(i+1   ) % len];
                var dx = prev.X - next.X;
                var dy = prev.Y - next.Y;
                var distSq = (dx*dx) + (dy*dy);
                

                if (distSq < 8)
                {
                    final.Add(new GlyphPoint { OnCurve = true,
                        X = (prev.X + mid.X + next.X) / 3.0,
                        Y = (prev.Y + mid.Y + next.Y) / 3.0 });
                }
                else
                {
                    var m0 = mid;
                    final.Add(new GlyphPoint { OnCurve = true, X = (prev.X + m0.X) / 2.0, Y = (prev.Y + m0.Y) / 2.0 });
                    final.Add(new GlyphPoint{ OnCurve = true, X = (next.X + m0.X) / 2.0, Y = (next.Y + m0.Y) / 2.0});
                }
            }

            return final.ToArray();
        }
    }
}