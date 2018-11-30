using FontReader.Read;

namespace FontReader.Draw
{
    /// <summary>
    /// Image renderers.
    /// These use the Scanline rasteriser internally
    /// </summary>
    public class Renderers
    {
        /// <summary>
        /// Render small font sizes with a super-sampling sub-pixel algorithm. Super-samples only in the x direction
        /// </summary>
        public static void RenderSubPixel_RGB_Super3(BitmapProxy img, float dx, float dy, float scale, Glyph glyph, bool inverted)
        {
            const int bright = 17;
            var bmp = BresenhamEdgeRasteriser.Render(glyph, scale * 3, scale, out var baseline, out var leftShift);
            var height = bmp.GetLength(0);
            var width = bmp.GetLength(1) / 3;
            if (baseline*baseline < 1) baseline = 0;

            var topsFlag = BresenhamEdgeRasteriser.DIR_RIGHT | BresenhamEdgeRasteriser.DIR_LEFT | BresenhamEdgeRasteriser.DROPOUT;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    // ReSharper disable JoinDeclarationAndInitializer
                    int tops,ins,left,right;
                    // ReSharper restore JoinDeclarationAndInitializer

                    var _1 = bmp[y, x*3];
                    var _2 = bmp[y, x*3 + 1];
                    var _3 = bmp[y, x*3 + 2];

                    // first try the simple case of all pixels in:
                    if (
                           (_1 & BresenhamEdgeRasteriser.INSIDE) > 0
                        && (_2 & BresenhamEdgeRasteriser.INSIDE) > 0
                        && (_3 & BresenhamEdgeRasteriser.INSIDE) > 0
                        ) {
                        var v = inverted ? 0 : 255;
                        img.SetPixel((int)(dx + x - leftShift), (int)(dy - y - baseline), v, v, v);
                        continue;
                    }
                    var topS = 3;
                    var insS = 5;
                    var sideS = 3;

                    var flag = _1;
                    tops = (flag & topsFlag) > 0 ? topS : 0;
                    ins = (flag & BresenhamEdgeRasteriser.INSIDE) > 0 ? insS : 0;
                    left = (flag & BresenhamEdgeRasteriser.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & BresenhamEdgeRasteriser.DIR_DOWN) > 0 ? sideS : 0;
                    if (ins > 0 || left > 0 || right > 0) tops = 0;

                    b += tops + ins + (left * 2);
                    g += tops + ins + (left) + (right);
                    r += tops + ins  + (right * 2);
                    
                    flag = _2;
                    tops = (flag & topsFlag) > 0 ? topS : 0;
                    ins = (flag & BresenhamEdgeRasteriser.INSIDE) > 0 ? insS : 0;
                    left = (flag & BresenhamEdgeRasteriser.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & BresenhamEdgeRasteriser.DIR_DOWN) > 0 ? sideS : 0;
                    if (ins > 0 || left > 0 || right > 0) tops = 0;

                    b += tops + ins + (left * 2);
                    g += tops + ins + (left) + (right);
                    r += tops + ins + (right * 2);
                    
                    flag = _3;
                    tops = (flag & topsFlag) > 0 ? topS : 0;
                    ins = (flag & BresenhamEdgeRasteriser.INSIDE) > 0 ? insS : 0;
                    left = (flag & BresenhamEdgeRasteriser.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & BresenhamEdgeRasteriser.DIR_DOWN) > 0 ? sideS : 0;
                    if (ins > 0 || left > 0 || right > 0) tops = 0;

                    b += tops + ins  + (left * 2);
                    g += tops + ins + left + right;
                    r += tops + ins + (right * 2);
                    
                    if (r == 0 && g == 0 && b == 0) continue;

                    r *= bright;
                    g *= bright;
                    b *= bright;
                    
                    if (inverted) {
                        b = 255 - b;
                        r = 255 - r;
                        g = 255 - g;
                    }

                    Saturate(ref r, ref g, ref b);

                    img.SetPixel((int)(dx + x - leftShift), (int)(dy - y - baseline), r, g, b);
                }
            }
        }

        /// <summary>
        /// Smoothing renderer for larger sizes. Does not gurantee sharp pixel edges, loses edges on small sizes
        /// </summary>
        public static void RenderSuperSampled(BitmapProxy img, float dx, float dy, float scale, Glyph glyph, bool inverted)
        {
            // Over-scale sizes. More y precision works well for latin glyphs
            const int xos = 2;
            const int yos = 3;

            // Render over-sized, then average back down
            var bmp = BresenhamEdgeRasteriser.Render(glyph, scale * xos, scale * yos, out var baseline, out var leftShift);
            var height = bmp.GetLength(0) / yos;
            var width = bmp.GetLength(1) / xos;
            baseline /= 2;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var sx = x*xos;
                    var sy = y*yos;
                    int v;
                    v  = bmp[sy  , sx  ] & BresenhamEdgeRasteriser.INSIDE; // based on `INSIDE` == 1
                    v += bmp[sy  , sx+1] & BresenhamEdgeRasteriser.INSIDE;
                    v += bmp[sy+1, sx  ] & BresenhamEdgeRasteriser.INSIDE;
                    v += bmp[sy+1, sx+1] & BresenhamEdgeRasteriser.INSIDE;
                    v += bmp[sy+2, sx  ] & BresenhamEdgeRasteriser.INSIDE;
                    v += bmp[sy+2, sx+1] & BresenhamEdgeRasteriser.INSIDE;

                    // slightly over-run in Y to smooth slopes further. The `ScanlineRasteriser` adds some buffer space for this
                    v += bmp[sy+3, sx  ] & BresenhamEdgeRasteriser.INSIDE;
                    v += bmp[sy+3, sx+1] & BresenhamEdgeRasteriser.INSIDE;
                    v += bmp[sy+4, sx  ] & BresenhamEdgeRasteriser.INSIDE;
                    v += bmp[sy+4, sx+1] & BresenhamEdgeRasteriser.INSIDE;

                    if (v == 0) continue;
                    v *= 255 / 10;
                    
                    if (inverted) {
                        v = 255 - v;
                    }

                    img.SetPixel((int)(dx + x - leftShift), (int)(dy - y - baseline), v, v, v);
                }
            }
        }

        private static void Saturate(ref int r, ref int g, ref int b)
        {
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;
            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;
        }
        
    }
}