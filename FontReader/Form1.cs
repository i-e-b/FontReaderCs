using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using FontReader.Draw;

namespace FontReader
{
    public partial class Form1 : Form
    {
        private static Pen ThinWhite;
        public Form1()
        {
            ThinWhite = new Pen(Color.White, 0.125f);
            InitializeComponent();
            TestRun();
        }

        private void TestRun()
        {
            if (outputPictureBox.Image != null) {
                outputPictureBox.Image.Dispose();
            }
            var img = new Bitmap(640, 480, PixelFormat.Format24bppRgb);

            var daveFnt = new TrueTypeFont("dave.ttf"); // a font that uses only straight edges (easy to render)
            var guthenFnt = new TrueTypeFont("guthen_bloots.ttf"); // a very curvy font (control points not yet supported)

            var msg_1 = "Hello, world! i$ ▚ ¾ ∜ -_¬~";
            var msg_2 = "Got to be funky";
            var msg_3 = "But, in a larger sense, we can not dedicate - we can not consecrate—we can not hallow—this ground. The brave men, living and dead,\n" +
                        "who struggled here, have consecrated it, far above our poor power to add or detract. The world will little note, nor long remember\n" +
                        "what we say here, but it can never forget what they did here.";


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
            scale = 0.01025f;
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

            outputPictureBox.Image = img;
            Width = img.Width + 18;
            Height = img.Height + 41;
        }


        private void DrawGlyph(Bitmap img, float dx, float dy, float scale, Glyph glyph)
        {
            try
            {
                // TODO: Move these rendering functions out of .Net code
                if (scale <= 0.02f)
                {
                    RenderSubPixel_RGB_Horz(img, dx, dy, scale, glyph); // Optimised for smaller sizes
                }
                else
                {
                    RenderSuperSampled(img, dx, dy, scale, glyph); // Optimised for larger sizes
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // tried to write off the end of the img
            }
        }

        /// <summary>
        /// Render small font sizes with a rough sub-pixel algorithm. Tends to give sharp edges
        /// </summary>
        private static void RenderSubPixel_RGB_Horz(Bitmap img, float dx, float dy, float scale, Glyph glyph)
        {
            var bmp = ScanlineRender.Render(glyph, scale, out var baseline);
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
            var bmp = ScanlineRender.Render(glyph, scale * 2, out var baseline);
            var height = bmp.GetLength(0) / 2;
            var width = bmp.GetLength(1) / 2;
            baseline /= 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int v;
                    v  = bmp[y*2  , x*2  ] & ScanlineRender.INSIDE; // based on `INSIDE` == 1
                    v += bmp[y*2+1, x*2  ] & ScanlineRender.INSIDE;
                    v += bmp[y*2  , x*2+1] & ScanlineRender.INSIDE;
                    v += bmp[y*2+1, x*2+1] & ScanlineRender.INSIDE;

                    if (v == 0) continue;
                    v *= 63; // 255 / number-of-samples

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

    }
}
