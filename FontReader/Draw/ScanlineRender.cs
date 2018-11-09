using System;
using System.Collections.Generic;
using System.Drawing;

namespace FontReader.Draw
{
    /// <summary>
    /// Render a single glyph using a non-zero-winding rule.
    /// This is an experiment to use a simplistic line-then-scanline method
    /// </summary>
    public class ScanlineRender
    {
        public const byte TOUCHED   = 0x01; // pixel has been processed

        public const byte WIND_UP   = 0x02; // pixel Y winding is 'up' (+1)
        public const byte WIND_DOWN = 0x04; // pixel Y winding is 'down' (-1)

        public const byte WIND_RITE = 0x08; // pixel X winding is 'right' (+1)
        public const byte WIND_LEFT = 0x10; // pixel X winding is 'left' (-1)

        public const byte INSIDE    = 0x20; // pixel is inside the glyph (for filling)
        public const byte DROPOUT   = 0x40; // pixel *might* be a small feature drop-out

        /// <summary>
        /// Render a glyph at the given scale. Result is a grid of flag values.
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

                    if (up) {w++; continue; }
                    if (dn && w > 0) {w--; continue; }

                    if (w > 0) workspace[y,x] |= INSIDE;
                }
                // if we get here, and the scan line is still on, scan back to the last change turning it back off
                if (w > 0) {
                    var changeFlags = WIND_UP | WIND_DOWN | DROPOUT;
                    for (int x = xmax-1; x > 0; x--)
                    {
                        if ((workspace[y,x] & changeFlags) > 0) break;
                        workspace[y,x] ^= INSIDE;
                    }
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
            for (int i = 0; i < len; i++)
            {
                var ptThis = contour[ i      % len];
                var ptNext = contour[(i + 1) % len];
                BresenhamWinding(workspace, ptThis, ptNext);
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

            int pxFlag = yWindFlag | xWindFlag | TOUCHED; // assume first pixel makes a full movement

            if (dy == 0 && dx == 0) pxFlag = DROPOUT | TOUCHED; // a single pixel. We mark for drop-out protection

            int err = (dx>dy ? dx : -dy) / 2;

            for(;;){ // for each point, bit-OR our decided direction onto the pixel

                // set pixel
                workspace[y0, x0] |= (byte)pxFlag;

                // end of line check
                if (x0==x1 && y0==y1) break;

                pxFlag = TOUCHED;
                var e2 = err;
                if (e2 >-dx) { err -= dy; x0 += sx; pxFlag |= xWindFlag; }
                if (e2 < dy) { err += dx; y0 += sy; pxFlag |= yWindFlag; }
            }
        }


    }
}