namespace FontReader.Draw
{

    /// <summary>
    /// Abstraction for editable bitmap images
    /// </summary>
    public abstract class BitmapProxy {
        public abstract void SetPixel(int x, int y, int r, int g, int b);
    }

    /// <summary>
    /// Image renderers
    /// </summary>
    public class Renderers
    {
        /// <summary>
        /// Render small font sizes with a super-sampling sub-pixel algorithm. Super-samples only in the x direction
        /// </summary>
        public static void RenderSubPixel_RGB_Super3(BitmapProxy img, float dx, float dy, float scale, Glyph glyph)
        {
            const int bright = 17;
            var bmp = ScanlineRasteriser.Render(glyph, scale * 3, scale, out var baseline);
            var height = bmp.GetLength(0);
            var width = bmp.GetLength(1) / 3;

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
                           (_1 & ScanlineRasteriser.INSIDE) > 0
                        && (_2 & ScanlineRasteriser.INSIDE) > 0
                        && (_3 & ScanlineRasteriser.INSIDE) > 0
                        ) {
                        img.SetPixel((int)dx + x, (int)(dy - y - baseline), 255, 255, 255);
                        continue;
                    }
                    var topS = 3;
                    var insS = 5;
                    var sideS = 3;

                    var flag = _1;
                    tops = ((flag & ScanlineRasteriser.DIR_RIGHT) > 0) || (flag & (ScanlineRasteriser.DIR_LEFT)) > 0 ? topS : 0;
                    ins = (flag & ScanlineRasteriser.INSIDE) > 0 ? insS : 0;
                    left = (flag & ScanlineRasteriser.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & ScanlineRasteriser.DIR_DOWN) > 0 ? sideS : 0;
                    if (ins > 0 || left > 0 || right > 0) tops = 0;

                    b += tops + ins + (left * 2);
                    g += tops + ins + (left) + (right);
                    r += tops + ins  + (right * 2);
                    
                    flag = _2;
                    tops = ((flag & ScanlineRasteriser.DIR_RIGHT) > 0) || (flag & (ScanlineRasteriser.DIR_LEFT)) > 0 ? topS : 0;
                    ins = (flag & ScanlineRasteriser.INSIDE) > 0 ? insS : 0;
                    left = (flag & ScanlineRasteriser.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & ScanlineRasteriser.DIR_DOWN) > 0 ? sideS : 0;
                    if (ins > 0 || left > 0 || right > 0) tops = 0;

                    b += tops + ins + (left * 2);
                    g += tops + ins + (left) + (right);
                    r += tops + ins + (right * 2);
                    
                    flag = _3;
                    tops = ((flag & ScanlineRasteriser.DIR_RIGHT) > 0) || (flag & (ScanlineRasteriser.DIR_LEFT)) > 0 ? topS : 0;
                    ins = (flag & ScanlineRasteriser.INSIDE) > 0 ? insS : 0;
                    left = (flag & ScanlineRasteriser.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & ScanlineRasteriser.DIR_DOWN) > 0 ? sideS : 0;
                    if (ins > 0 || left > 0 || right > 0) tops = 0;

                    b += tops + ins  + (left * 2);
                    g += tops + ins + left + right;
                    r += tops + ins + (right * 2);

                    if (r == 0 && g == 0 && b == 0) continue;

                    r *= bright;
                    g *= bright;
                    b *= bright;
                    
                    Saturate(ref r, ref g, ref b);

                    img.SetPixel((int)dx + x, (int)(dy - y - baseline), r, g, b);
                }
            }
        }

        /// <summary>
        /// Render small font sizes with a rough sub-pixel algorithm, based on edge direction.
        /// </summary>
        public static void RenderSubPixel_RGB_Edge(BitmapProxy img, float dx, float dy, float scale, Glyph glyph)
        {
            var bmp = ScanlineRasteriser.Render(glyph, scale, scale, out var baseline);
            var height = bmp.GetLength(0);
            var width = bmp.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var v = bmp[y, x];
                    if (v == 0) continue;

                    var r = 0;
                    var g = 0;
                    var b = 0;

                    bool vert = false;
                    var up = (v & ScanlineRasteriser.DIR_UP) > 0;
                    var down = (v & ScanlineRasteriser.DIR_DOWN) > 0;
                    var left = (v & ScanlineRasteriser.DIR_LEFT) > 0;
                    var right = (v & ScanlineRasteriser.DIR_RIGHT) > 0;
                    var inside = (v & ScanlineRasteriser.INSIDE) > 0;

                    if (up) { r += 0; g += 160; b += 255; vert = true; }
                    else if (down) { r += 255; g += 100; b += 0; vert = true; }

                    if (!vert)
                    {
                        if (right) { r += 127; g += 127; b += 127; } // top edge
                        if (left) { r += 127; g += 127; b += 127; } // bottom edge
                    }

                    if (inside) { r += 255; g += 255; b += 255; }

                    Saturate(ref r, ref g, ref b);

                    if ((r + g + b) == 0 && (v & ScanlineRasteriser.DROPOUT) > 0) { r += 255; g += 255; b += 255; }

                    img.SetPixel((int)dx + x, (int)(dy - y - baseline), r, g, b);
                }
            }
        }

        /// <summary>
        /// Smoothing renderer for larger sizes. Does not gurantee sharp pixel edges, loses edges on small sizes
        /// </summary>
        public static void RenderSuperSampled(BitmapProxy img, float dx, float dy, float scale, Glyph glyph)
        {
            // Render double-sized, then average back down
            var bmp = ScanlineRasteriser.Render(glyph, scale * 2f, scale * 2f, out var baseline);
            var height = bmp.GetLength(0) / 2;
            var width = bmp.GetLength(1) / 2;
            baseline /= 2;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int v;
                    v  = bmp[y*2  , x*2  ] & ScanlineRasteriser.INSIDE; // based on `INSIDE` == 1
                    v += bmp[y*2  , x*2+1] & ScanlineRasteriser.INSIDE;
                    v += bmp[y*2+1, x*2+1] & ScanlineRasteriser.INSIDE;
                    v += bmp[y*2+1, x*2  ] & ScanlineRasteriser.INSIDE;
                    v += bmp[y*2+2, x*2+1] & ScanlineRasteriser.INSIDE;
                    v += bmp[y*2+2, x*2  ] & ScanlineRasteriser.INSIDE;

                    if (v == 0) continue;
                    v *= 255 / 6;

                    img.SetPixel((int)dx + x, (int)(dy - y - baseline), v, v, v);
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
        
        private static void Normalise(ref int r, ref int g, ref int b)
        {
            var max = r;
            if (g > max) max = g;
            if (b > max) max = b;
            var fact = 255 / max;
            r *= fact;
            g *= fact;
            b *= fact;
        } 
    }
}