using System;
using FontReader.Read;

namespace FontReader.Draw
{
    /// <summary>
    /// Render a single glyph using a direction and scan-line rule.
    /// </summary>
    public class BresenhamEdgeRasteriser
    {
        public const byte INSIDE    = 0x01; // pixel is inside the glyph (for filling)

        public const byte DIR_UP    = 0x02; // pixel Y direction is 'up' (+1)
        public const byte DIR_DOWN  = 0x04; // pixel Y direction is 'down' (-1)

        public const byte DIR_RIGHT = 0x08; // pixel X direction is 'right' (+1)
        public const byte DIR_LEFT  = 0x10; // pixel X direction is 'left' (-1)

        public const byte TOUCHED   = 0x20; // pixel has been processed
        public const byte DROPOUT   = 0x40; // pixel *might* be a small feature drop-out

        /// <summary>
        /// Render a glyph at the given scale. Result is a grid of flag values.
        /// </summary>
        /// <param name="glyph">Glyph to render</param>
        /// <param name="xScale">Scale factor</param>
        /// <param name="yScale">Scale factor</param>
        /// <param name="baseline">Offset from grid bottom to baseline</param>
        /// <param name="leftShift">Offset from grid left to character left</param>
        public static byte[,] Render(Glyph glyph, float xScale, float yScale, out float baseline, out float leftShift){
            baseline = 0;
            leftShift = 0;
            if (glyph == null) return null;
            if (glyph.GlyphType != GlyphTypes.Simple) return new byte[0, 0];

            // glyph sizes are not reliable for this.
            glyph.GetPointBounds(out var xmin, out var xmax, out var ymin, out var ymax);

            baseline = (float) (glyph.yMin * yScale);
            leftShift = (float)(-glyph.xMin * xScale * 0.5); // I guess the 0.5 fudge-factor is due to lack of kerning support

            var width = (int)((xmax - xmin) * xScale) + 2;
            var height = (int)((ymax - ymin) * yScale) + 2;
            var yAdjust = (baseline < 0) ? -0.5f : 0.0f;

            var workspace = new byte[height, width];

            // 1. Walk around all the contours, setting scan-line winding data.
            WalkContours(glyph, xScale, yScale, yAdjust, workspace);

            // 2. Run each scanline, filling where sum of winding is != 0
            FillScans(workspace);
            //DiagnosticFillScans(workspace);

            return workspace;
        }

        private static void DiagnosticFillScans(byte[,] workspace)
        {
            if (workspace == null) return;
            var ymax = workspace.GetLength(0);
            var xmax = workspace.GetLength(1) - 1; // space to look ahead

            for (int y = 0; y < ymax; y++)
            {
                int w = 0;
                bool prevUp = false;
                for (int x = 0; x < xmax; x++)
                {
                    if (workspace[y, x] != 0) workspace[y, x] |= INSIDE;
                }
            }
        }

        private static void FillScans(byte[,] workspace)
        {
            if (workspace == null) return;
            var ymax = workspace.GetLength(0);
            var xmax = workspace.GetLength(1) - 1; // space to look ahead

            for (int y = 0; y < ymax; y++)
            {
                int w = 0;
                bool prevUp = false;
                for (int x = 0; x < xmax; x++)
                {
                    var v = workspace[y,x];
                    var up = (v & DIR_UP) > 0;
                    var dn = (v & DIR_DOWN) > 0;

                    if (up && dn) {
                        prevUp = false;
                        workspace[y,x] |= INSIDE;
                        continue;
                    }

                    if (up) {w=1; }
                    if (dn) {w=0; }

                    var nextUp =  (workspace[y,x+1] & DIR_UP) > 0;
                    var nextDown =  (workspace[y,x+1] & DIR_DOWN) > 0;

                    if (!up && !dn && w > 0) workspace[y, x] |= INSIDE;
                    if (prevUp && dn && nextDown && !nextUp) workspace[y, x] |= INSIDE;
                    prevUp = up;
                }
            }
        }

        private static void WalkContours(Glyph glyph, float xScale, float yScale, float yAdjust, byte[,] workspace)
        {
            if (glyph == null) return;
            if (glyph.Points == null || glyph.Points.Length < 1) return;
            if (glyph.ContourEnds == null || glyph.ContourEnds.Length < 1) return;

            var contours = glyph.NormalisedContours(xScale, yScale, 0, yAdjust);
            if (contours == null) return;
            foreach (var contour in contours)
            {
                RenderContour(workspace, contour);
            }
        }

        private static void RenderContour(byte[,] workspace, GlyphPoint[] contour)
        {
            if (contour == null) return;
            if (workspace == null) return;

            var len = contour.Length;
            for (int i = 0; i < len; i++)
            {
                var ptThis = contour[ i      % len];
                var ptNext = contour[(i + 1) % len];
                DirectionalBresenham(workspace, ptThis, ptNext);
            } 
        }

        /// <summary>
        /// Write directions between two points into the workspace.
        /// </summary>
        private static void DirectionalBresenham(byte[,] workspace, GlyphPoint start, GlyphPoint end)
        {
            if (workspace == null) return;

            var fdx = end.X - start.X;
            var fdy = end.Y - start.Y;

            int x0 = (int)Math.Round(start.X);
            int y0 = (int)Math.Round(start.Y);
            int x1 = (int)Math.Round(end.X);
            int y1 = (int)Math.Round(end.Y);

            int dx = x1-x0, sx = x0<x1 ? 1 : -1;
            int dy = y1-y0, sy = y0<y1 ? 1 : -1;
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;

            byte xWindFlag = fdx < 0 ? DIR_LEFT : DIR_RIGHT;
            byte yWindFlag = fdy < 0 ? DIR_DOWN : DIR_UP;
            if (dy == 0) yWindFlag = 0;
            if (dx == 0) xWindFlag = 0;

            int pxFlag = yWindFlag | xWindFlag | TOUCHED; // assume first pixel makes a full movement

            if (dy == 0 && dx == 0)
                pxFlag |= DROPOUT; // a single pixel. We mark for drop-out protection

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