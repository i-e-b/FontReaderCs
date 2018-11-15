using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using FontReader.Draw;
using FontReader.Read;

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
            using(var g = Graphics.FromImage(img)){
                g.FillRectangle(Brushes.White, 0, 400, 1024, 600);
            }

            var daveFnt = new TrueTypeFont("dave.ttf"); // a font that uses only straight edges (easy to render)
            //var daveFnt = new TrueTypeFont(@"C:\Temp\what.bin"); // other fonts
            var guthenFnt = new TrueTypeFont("guthen_bloots.ttf"); // a curvy font

            var msg_1 = "Hello, world! i0($} ▚ ¾ ∜ -_¬~";
            var msg_2 = "Got to be funky. CQUPOJ8";
            var msg_3 = "0123456789\nBut, in a larger sense, we can not dedicate - we can not consecrate—we can not hallow—this ground. The brave men,\n" +
                        "living and dead, who struggled here, have consecrated it, far above our poor power to add or detract. The world will\n" +
                        "little note, nor long remember what we say here, but it can never forget what they did here.";


            // Draw first message with angular font
            float left = 25;
            float baseline = 150f;
            float scale = 48f / daveFnt.Height();
            float letterSpace = 5;
            for (int i = 0; i < msg_1.Length; i++)
            {
                var glyph = daveFnt.ReadGlyph(msg_1[i]);

                DrawGlyph(img, left, baseline, scale, glyph, false);
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

                DrawGlyph(img, left, baseline, scale, glyph, false);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }

            // Draw a much longer message in the angular font
            left = 5;
            baseline = 50f;
            scale = 16f / daveFnt.Height();
            letterSpace = 1.6f;
            for (int i = 0; i < msg_3.Length; i++)
            {
                if (msg_3[i] == '\n')
                {
                    left = 5; baseline += 16; continue;
                }
                var glyph = daveFnt.ReadGlyph(msg_3[i]);

                DrawGlyph(img, left, baseline, scale, glyph, false);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }
            
            var prox = new FormsBitmap(img);
            
            // Show a sample of the two sub-pixel AA strategies
            left = 5;
            baseline = 300f;
            scale = 16f / daveFnt.Height(); // about 11pt
            letterSpace = 2;
            for (int i = 0; i < 58; i++)
            {
                var glyph = daveFnt.ReadGlyph((char) ('A'+i));
                Renderers.RenderSubPixel_RGB_Super3(prox, left, baseline + 20, scale, glyph, false); // this looks reasonable
                Renderers.RenderSubPixel_RGB_Edge  (prox, left, baseline + 40, scale, glyph, false);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }
            
            left = 5;
            baseline = 350f;
            scale = 10f / daveFnt.Height(); // very small.
            letterSpace = 1.5f;
            for (int i = 0; i < 58; i++)
            {
                var glyph = daveFnt.ReadGlyph((char) ('A'+i));
                Renderers.RenderSubPixel_RGB_Super3(prox, left, baseline + 30, scale, glyph, false);
                Renderers.RenderSubPixel_RGB_Edge  (prox, left, baseline + 42, scale, glyph, false); // this shines at tiny sizes
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }


            // Demonstrate black-on-white
            left = 5;
            baseline = 400f;
            scale = 16f / daveFnt.Height(); // about 11pt
            letterSpace = 2.1f;
            for (int i = 0; i < 58; i++)
            {
                var glyph = daveFnt.ReadGlyph((char) ('A'+i));
                Renderers.RenderSubPixel_RGB_Super3(prox, left, baseline + 20, scale, glyph, true);
                Renderers.RenderSubPixel_RGB_Edge  (prox, left, baseline + 38, scale, glyph, true);
                Renderers.RenderSubPixel_RGB_Super3(prox, left, baseline + 50, scale *0.6f, glyph, true);
                Renderers.RenderSubPixel_RGB_Edge  (prox, left, baseline + 58, scale *0.6f, glyph, true);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }
            
            // Large black-on-white
            left = 25;
            baseline = 540f;
            scale = 70f / daveFnt.Height();
            letterSpace = 5;
            for (int i = 0; i < msg_1.Length; i++)
            {
                var glyph = daveFnt.ReadGlyph(msg_1[i]);

                DrawGlyph(img, left, baseline, scale, glyph, true);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }

            outputPictureBox.Image = img;
            Width = img.Width + 18;
            Height = img.Height + 41;
        }

        private void DrawGlyph(Bitmap img, float dx, float dy, float scale, Glyph glyph, bool inverted)
        {
            try
            {
                var prox = new FormsBitmap(img);
                if (scale <= 0.04f) // Optimised for smaller sizes
                {
                    //RenderSubPixel_RGB_Super2(img, dx, dy, scale, glyph);
                    Renderers.RenderSubPixel_RGB_Super3(prox, dx, dy, scale, glyph, inverted);
                    //RenderSubPixel_RGB_Edge(img, dx, dy, scale, glyph);
                }
                else // Optimised for larger sizes
                {
                    Renderers.RenderSuperSampled(prox, dx, dy, scale, glyph, inverted);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // tried to write off the end of the img
            }
        }



    }

    internal class FormsBitmap : BitmapProxy
    {
        private readonly Bitmap _img;

        public FormsBitmap(Bitmap img)
        {
            _img = img;
        }

        public override void SetPixel(int x, int y, int r, int g, int b)
        {
            if (x < 0 || y < 0 || x >= _img.Width || y >= _img.Height) return;
            _img.SetPixel(x,y, Color.FromArgb(r,g,b));
        }
    }
}
