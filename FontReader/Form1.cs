using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using FontReader.Draw;

namespace FontReader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            TestRun();
        }
        private void TestRun()
        {
            if (outputPictureBox.Image != null) {
                outputPictureBox.Image.Dispose();
            }
            var img = new Bitmap(1024, 600, PixelFormat.Format24bppRgb);

            var daveFnt = new TrueTypeFont("dave.ttf"); // a font that uses only straight edges (easy to render)
            var guthenFnt = new TrueTypeFont("guthen_bloots.ttf"); // a very curvy font (control points not yet supported)

            var msg_1 = "Hello, world! i$ ▚ ¾ ∜ -_¬~";
            var msg_2 = "Got to be funky";
            var msg_3 = "But, in a larger sense, we can not dedicate - we can not consecrate—we can not hallow—this ground. The brave men,\n" +
                        "living and dead, who struggled here, have consecrated it, far above our poor power to add or detract. The world will\n" +
                        "little note, nor long remember what we say here, but it can never forget what they did here.";


            // Draw first message with angular font
            float left = 25;
            float baseline = 150f;
            float scale = 0.05f;
            float letterSpace = 5;
            for (int i = 0; i < msg_1.Length; i++)
            {
                var glyph = daveFnt.ReadGlyph(msg_1[i]);

                DrawGlyph(img, left, baseline, scale, glyph);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }

            // Draw second message with curvy font
            left = 25;
            baseline = 250f;
            scale = 0.05f;
            for (int i = 0; i < msg_2.Length; i++)
            {
                var glyph = guthenFnt.ReadGlyph(msg_2[i]);

                DrawGlyph(img, left, baseline, scale, glyph);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }

            // Draw a much longer message in the angular font
            left = 5;
            baseline = 50f;
            scale = 0.016f;
            letterSpace = 1;
            for (int i = 0; i < msg_3.Length; i++)
            {
                if (msg_3[i] == '\n')
                {
                    left = 5; baseline += 16; continue;
                }
                var glyph = daveFnt.ReadGlyph(msg_3[i]);

                DrawGlyph(img, left, baseline, scale, glyph);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }

            
            left = 5;
            baseline = 300f;
            scale = 16f / 1024f;
            letterSpace = 2;
            // Show a sample of each small AA strategy:
            for (int i = 0; i < 58; i++)
            {
                var glyph = daveFnt.ReadGlyph((char) ('A'+i));
                RenderSubPixel_RGB_Super2(img, left, baseline +  0, scale, glyph);
                RenderSubPixel_RGB_Super3(img, left, baseline + 20, scale, glyph);
                RenderSubPixel_RGB_Edge  (img, left, baseline + 40, scale, glyph);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }
            
            left = 5;
            baseline = 400f;
            scale = 32f / 1024f;
            letterSpace = 2;
            // Show a sample of each small AA strategy:
            for (int i = 0; i < 58; i++)
            {
                var glyph = daveFnt.ReadGlyph((char) ('A'+i));
                RenderSubPixel_RGB_Super2(img, left, baseline +  0, scale, glyph);
                RenderSubPixel_RGB_Super3(img, left, baseline + 30, scale, glyph);
                RenderSubPixel_RGB_Edge  (img, left, baseline + 60, scale, glyph);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }

            outputPictureBox.Image = img;
            Width = img.Width + 18;
            Height = img.Height + 41;
        }

        private void DrawGlyph(Bitmap img, float dx, float dy, float scale, Glyph glyph)
        {
            try
            {
                // TODO: Move these rendering functions out of WinForms code
                if (scale <= 0.04f) // Optimised for smaller sizes
                {
                    //RenderSubPixel_RGB_Super2(img, dx, dy, scale, glyph);
                    RenderSubPixel_RGB_Super3(img, dx, dy, scale, glyph);
                    //RenderSubPixel_RGB_Edge(img, dx, dy, scale, glyph);
                }
                else // Optimised for larger sizes
                {
                    RenderSuperSampled(img, dx, dy, scale, glyph);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // tried to write off the end of the img
            }
        }

        /// <summary>
        /// Render small font sizes with a super-sampling sub-pixel algorithm. Super-samples only in the x direction
        /// </summary>
        private static void RenderSubPixel_RGB_Super3(Bitmap img, float dx, float dy, float scale, Glyph glyph)
        {
            var bmp = ScanlineRender.Render(glyph, scale * 3, scale, out var baseline);
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
                           (_1 & ScanlineRender.INSIDE) > 0
                        && (_2 & ScanlineRender.INSIDE) > 0
                        && (_3 & ScanlineRender.INSIDE) > 0
                        ) {
                        img.SetPixel((int)dx + x, (int)(dy - y - baseline), Color.FromArgb(255, 255, 255));
                        continue;
                    }
                    var topS = 3;
                    var insS = 5;
                    var sideS = 3;

                    var flag = _1;
                    tops = ((flag & ScanlineRender.DIR_RIGHT) > 0) || (flag & (ScanlineRender.DIR_LEFT)) > 0 ? topS : 0;
                    ins = (flag & ScanlineRender.INSIDE) > 0 ? insS : 0;
                    left = (flag & ScanlineRender.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & ScanlineRender.DIR_DOWN) > 0 ? sideS : 0;
                    if (ins > 0 || left > 0 || right > 0) tops = 0;

                    b += tops + ins + (left * 2);
                    g += tops + ins + (left) + (right);
                    r += tops + ins  + (right * 2);
                    
                    flag = _2;
                    tops = ((flag & ScanlineRender.DIR_RIGHT) > 0) || (flag & (ScanlineRender.DIR_LEFT)) > 0 ? topS : 0;
                    ins = (flag & ScanlineRender.INSIDE) > 0 ? insS : 0;
                    left = (flag & ScanlineRender.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & ScanlineRender.DIR_DOWN) > 0 ? sideS : 0;
                    if (ins > 0 || left > 0 || right > 0) tops = 0;

                    b += tops + ins + (left * 2);
                    g += tops + ins + (left) + (right);
                    r += tops + ins + (right * 2);
                    
                    flag = _3;
                    tops = ((flag & ScanlineRender.DIR_RIGHT) > 0) || (flag & (ScanlineRender.DIR_LEFT)) > 0 ? topS : 0;
                    ins = (flag & ScanlineRender.INSIDE) > 0 ? insS : 0;
                    left = (flag & ScanlineRender.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & ScanlineRender.DIR_DOWN) > 0 ? sideS : 0;
                    if (ins > 0 || left > 0 || right > 0) tops = 0;

                    b += tops + ins  + (left * 2);
                    g += tops + ins + (left) + (right);
                    r += tops + ins + (right * 2);

                    if (r == 0 && g == 0 && b == 0) continue;

                    var bright = 20;
                    r *= bright;
                    g *= bright;
                    b *= bright;
                    
                    Saturate(ref r, ref g, ref b);

                    img.SetPixel((int)dx + x, (int)(dy - y - baseline), Color.FromArgb(r, g, b));
                }
            }
        }

        
        /// <summary>
        /// Render small font sizes with a super-sampling sub-pixel algorithm. Super-samples only in the x direction
        /// </summary>
        private static void RenderSubPixel_RGB_Super2(Bitmap img, float dx, float dy, float scale, Glyph glyph)
        {
            var bmp = ScanlineRender.Render(glyph, scale * 2, scale, out var baseline);
            var height = bmp.GetLength(0);
            var width = bmp.GetLength(1) / 2;

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

                    var _1 = bmp[y, x*2];
                    var _2 = bmp[y, x*2 + 1];

                    // first try the simple case of all pixels in:
                    if (
                           (_1 & ScanlineRender.INSIDE) > 0
                        && (_2 & ScanlineRender.INSIDE) > 0
                        ) {
                        img.SetPixel((int)dx + x, (int)(dy - y - baseline), Color.FromArgb(255, 255, 255));
                        continue;
                    }
                    var topS = 3;
                    var insS = 5;
                    var sideS = 3;

                    var flag = _1;
                    tops = ((flag & ScanlineRender.DIR_RIGHT) > 0) || (flag & (ScanlineRender.DIR_LEFT)) > 0 ? topS : 0;
                    ins = (flag & ScanlineRender.INSIDE) > 0 ? insS : 0;
                    left = (flag & ScanlineRender.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & ScanlineRender.DIR_DOWN) > 0 ? sideS : 0;

                    r += tops + ins  + (right * 2);
                    g += tops + ins + (left) + (right);
                    b += tops + ins + (left * 2);
                    
                    flag = _2;
                    tops = ((flag & ScanlineRender.DIR_RIGHT) > 0) && (flag & (ScanlineRender.DIR_LEFT)) > 0 ? topS : 0;
                    ins = (flag & ScanlineRender.INSIDE) > 0 ? insS : 0;
                    left = (flag & ScanlineRender.DIR_UP) > 0 ? sideS : 0;
                    right = (flag & ScanlineRender.DIR_DOWN) > 0 ? sideS : 0;

                    r += tops + ins + (right * 2);
                    g += tops + ins + (left) + (right);
                    b += tops + ins + (left * 2);

                    if (r == 0 && g == 0 && b == 0) continue;

                    var bright = 25;
                    r *= bright;
                    g *= bright;
                    b *= bright;
                    
                    Saturate(ref r, ref g, ref b);

                    img.SetPixel((int)dx + x, (int)(dy - y - baseline), Color.FromArgb(r, g, b));
                }
            }
        }


        /// <summary>
        /// Render small font sizes with a rough sub-pixel algorithm, based on edge direction.
        /// </summary>
        private static void RenderSubPixel_RGB_Edge(Bitmap img, float dx, float dy, float scale, Glyph glyph)
        {
            var bmp = ScanlineRender.Render(glyph, scale, scale, out var baseline);
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
                    var up = (v & ScanlineRender.DIR_UP) > 0;
                    var down = (v & ScanlineRender.DIR_DOWN) > 0;
                    var left = (v & ScanlineRender.DIR_LEFT) > 0;
                    var right = (v & ScanlineRender.DIR_RIGHT) > 0;
                    var inside = (v & ScanlineRender.INSIDE) > 0;

                    if (up) { r += 0; g += 160; b += 255; vert = true; }
                    else if (down) { r += 255; g += 100; b += 0; vert = true; }

                    if (!vert)
                    {
                        if (right) { r += 127; g += 127; b += 127; } // top edge
                        if (left) { r += 127; g += 127; b += 127; } // bottom edge
                    }

                    if (inside) { r += 255; g += 255; b += 255; }

                    Saturate(ref r, ref g, ref b);

                    if ((r + g + b) == 0 && (v & ScanlineRender.DROPOUT) > 0) { r += 255; g += 255; b += 255; }

                    img.SetPixel((int)dx + x, (int)(dy - y - baseline), Color.FromArgb(r, g, b));
                }
            }
        }

        /// <summary>
        /// Smoothing renderer for larger sizes. Does not gurantee sharp pixel edges, loses edges on small sizes
        /// </summary>
        private static void RenderSuperSampled(Bitmap img, float dx, float dy, float scale, Glyph glyph)
        {
            // Render double-sized, then average back down
            var bmp = ScanlineRender.Render(glyph, scale * 2f, scale * 2f, out var baseline);
            var height = bmp.GetLength(0) / 2;
            var width = bmp.GetLength(1) / 2;
            baseline /= 2;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int v;
                    v  = bmp[y*2  , x*2  ] & ScanlineRender.INSIDE; // based on `INSIDE` == 1
                    v += bmp[y*2  , x*2+1] & ScanlineRender.INSIDE;
                    v += bmp[y*2+1, x*2+1] & ScanlineRender.INSIDE;
                    v += bmp[y*2+1, x*2  ] & ScanlineRender.INSIDE;
                    v += bmp[y*2+2, x*2+1] & ScanlineRender.INSIDE;
                    v += bmp[y*2+2, x*2  ] & ScanlineRender.INSIDE;

                    if (v == 0) continue;
                    v *= 255 / 6;

                    img.SetPixel((int)dx + x, (int)(dy - y - baseline), Color.FromArgb(v, v, v));
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
