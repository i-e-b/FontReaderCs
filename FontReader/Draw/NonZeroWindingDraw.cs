using System;
using System.Collections.Generic;
using System.Drawing;

namespace FontReader.Draw
{
    /// <summary>
    /// Render a single glyph using a non-zero-winding rule.
    /// This is an experiment to use a simplistic line-then-scanline method
    /// </summary>
    public class NonZeroWindingDraw
    {
        public const byte TOUCHED   = 0x01; // pixel has been processed

        public const byte WIND_UP   = 0x02; // pixel Y winding is 'up' (+1)
        public const byte WIND_DOWN = 0x04; // pixel Y winding is 'down' (-1)

        public const byte WIND_RITE = 0x08; // pixel X winding is 'right' (+1)
        public const byte WIND_LEFT = 0x10; // pixel X winding is 'left' (-1)

        public const byte INSIDE    = 0x20; // pixel is inside the glyph (for filling)

        /// <summary>
        /// Render a glyph at the given scale. Result is a grid of alpha values: 0-for-out, 255-for-in.
        /// </summary>
        /// <param name="glyph">Glyph to render</param>
        /// <param name="scale">Scale factor</param>
        /// <param name="baseline">Offset from grid bottom to baseline</param>
        public static byte[,] Render(Glyph glyph, float scale, out float baseline){
            baseline = 0;
            if (glyph == null) return null;
            if (glyph.GlyphType != GlyphTypes.Simple) return new byte[0, 0];

            // glyph sizes are not reliable for this.
            GetPointBounds(glyph, out var xmin, out var xmax, out var ymin, out var ymax);
            baseline = (float) (ymin * scale);

            var width = (int)((xmax - xmin) * scale) + 2;
            var height = (int)((ymax - ymin) * scale) + 2;

            var workspace = new byte[height, width];

            // 1. Walk around all the contours, setting scan-line winding data.
            WalkContours(glyph, scale, workspace);

            // 2. Run each scanline, filling where sum of winding is != 0
            FillScans(workspace);

            return workspace;
        }

        private static void FillScans(byte[,] workspace)
        {
            if (workspace == null) return;
            var ymax = workspace.GetLength(0);
            var xmax = workspace.GetLength(1);

            for (int y = 0; y < ymax; y++)
            {
                int w = 0;
                for (int x = 0; x < xmax; x++)
                {
                    var v = workspace[y,x];
                    var up = (v & WIND_UP) > 0;
                    var dn = (v & WIND_DOWN) > 0;

                    if (up && dn) {
                        workspace[y,x] |= INSIDE;
                        continue;
                    }

                    if (up) w = 1;
                    if (dn) w = 0;
                    if (w != 0) workspace[y,x] |= INSIDE;
                    //if (w == 0) workspace[y,x] |= INSIDE; // inverted filling.
                }
            }
        }

        private static void GetPointBounds(Glyph glyph, out double xmin, out double xmax, out double ymin, out double ymax)
        {
            xmin = 0d;
            xmax = 4d;
            ymin = 0d;
            ymax = 4d;
            if (glyph == null || glyph.Points == null) return;
            for (int i = 0; i < glyph.Points.Length; i++)
            {
                var p = glyph.Points[i];
                if (p == null) continue;
                xmin = Math.Min(p.X, xmin);
                xmax = Math.Max(p.X, xmax);
                ymin = Math.Min(p.Y, ymin);
                ymax = Math.Max(p.Y, ymax);
            }
        }

        private static void WalkContours(Glyph glyph, float scale, byte[,] workspace)
        {
            if (glyph == null) return;
            if (glyph.Points == null || glyph.Points.Length < 1) return;
            if (glyph.ContourEnds == null || glyph.ContourEnds.Length < 1) return;

            var p = 0;
            var c = 0;
            var contour = new List<PointF>();

            while (p < glyph.Points.Length)
            {
                var point = glyph.Points[p];
                if (point != null)
                {
                    var xpos = (point.X - glyph.xMin) * scale;
                    var ypos = (point.Y - glyph.yMin) * scale;

                    if (xpos < 0) xpos = 0;
                    if (ypos < 0) ypos = 0;

                    contour.Add(new PointF((float)xpos, (float)ypos));
                }

                if (p == glyph.ContourEnds[c])
                {
                    RenderContour(workspace, contour);
                    contour.Clear();
                    c++;
                }

                p++;
            }
        }

        private static void RenderContour(byte[,] workspace, List<PointF> contour)
        {
            if (contour == null) return;
            if (workspace == null) return;

            var len = contour.Count;
            for (int i = 1; i < len+1; i++)
            {
                var ptPrev = contour[(i - 1) % len];
                var ptThis = contour[ i      % len];
                var ptNext = contour[(i + 1) % len];

                // Mark the lines between points, not including the points themselves
                BresenhamWinding(workspace, ptThis, ptNext);

                // Mark the inflection points directly
                var xp = (int)ptPrev.X;
                var yp = (int)ptPrev.Y;
                var xi = (int)ptThis.X;
                var yi = (int)ptThis.Y;
                var xn = (int)ptNext.X;
                var yn = (int)ptNext.Y;

                if (workspace[yi,xi] == TOUCHED) continue;
                workspace[yi,xi] |= TOUCHED;
                // inspect each inflection
                
                if (yi < yp && yi < yn) workspace[yi,xi] |= WIND_DOWN | WIND_UP;
                if (yi > yp && yi > yn) workspace[yi,xi] |= WIND_DOWN | WIND_UP;

                // from point to next
                if (xn > xi) workspace[yi,xi] |= WIND_RITE;
                if (xn < xi) workspace[yi,xi] |= WIND_LEFT;
                if (yn > yi) workspace[yi,xi] |= WIND_UP;
                if (yn < yi) workspace[yi,xi] |= WIND_DOWN;
                // from prev to point
                if (xi > xp) workspace[yi,xi] |= WIND_RITE;
                if (xi < xp) workspace[yi,xi] |= WIND_LEFT;
                if (yi > yp) workspace[yi,xi] |= WIND_UP;
                if (yi < yp) workspace[yi,xi] |= WIND_DOWN;
                // from prev to next
                if (xn > xp) workspace[yi,xi] |= WIND_RITE;
                if (xn < xp) workspace[yi,xi] |= WIND_LEFT;
                if (yn > yp) workspace[yi,xi] |= WIND_UP;
                if (yn < yp) workspace[yi,xi] |= WIND_DOWN;
                
            } 
                
        }

        /// <summary>
        /// Write winding directions between two points into the workspace.
        /// The first and last points are excluded, to be handled as special cases.
        /// </summary>
        private static void BresenhamWinding(byte[,] workspace, PointF start, PointF end)
        {
            if (workspace == null) return;

            int x0 = (int)start.X;
            int y0 = (int)start.Y;
            int x1 = (int)end.X;
            int y1 = (int)end.Y;

            int dx = x1-x0, sx = x0<x1 ? 1 : -1;
            int dy = y1-y0, sy = y0<y1 ? 1 : -1;
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;

            // set the winding flags we are going to set based on `sx` and `sy`
            byte xWindFlag = sx < 0 ? WIND_LEFT : WIND_RITE;
            byte yWindFlag = sy < 0 ? WIND_DOWN : WIND_UP;
            if (dy == 0) yWindFlag = 0;
            if (dx == 0) xWindFlag = 0;

            int pxFlag = workspace[y0, x0]; // exclude the first pixel (set equal to self)
            //int pxFlag = yWindFlag | xWindFlag | TOUCHED; // assume first pixel makes a full movement

            int err = (dx>dy ? dx : -dy) / 2;

            for(;;){ // for each point, bit-OR our decided direction onto the pixel
                // end of line check
                if (x0==x1 && y0==y1) break; // having the check before the write excludes the last pixel

                // set pixel
                workspace[y0, x0] |= (byte)pxFlag;

                pxFlag = TOUCHED;

                var e2 = err;
                if (e2 >-dx) { err -= dy; x0 += sx; pxFlag |= xWindFlag; }
                if (e2 < dy) { err += dx; y0 += sy; pxFlag |= yWindFlag; }
            }
        }


    }
}