﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using FontReader.Draw;
using FontReader.Read;

namespace FontReader
{
    public partial class Form1 : Form
    {
        FontInfoWindow _infoWindow;
        private GlyphView _glyphWindow;


        public Form1()
        {
            InitializeComponent();
            TestRun();
        }

        private void TestRun()
        {
            outputPictureBox?.Image?.Dispose();
            var img = new Bitmap(1024, 600, PixelFormat.Format24bppRgb);
            using(var g = Graphics.FromImage(img)){
                g.FillRectangle(Brushes.White, 0, 400, 1024, 600);
            }

            var daveFnt = new TrueTypeFont("dave.ttf");             // a font that uses only straight edges (easy to render)
            var notoFnt = new TrueTypeFont("NotoSans-Regular.ttf"); // standard professional font
            var bendyFnt = new TrueTypeFont("bendy.ttf");           // a font with extreme curves for testing segmentation
            var guthenFnt = new TrueTypeFont("guthen_bloots.ttf");  // a curvy font
            
            _infoWindow = new FontInfoWindow();
            _infoWindow.SetFont(daveFnt);
            _infoWindow.Show();
            
            _glyphWindow = new GlyphView();
            _glyphWindow.SetFont(notoFnt);
            _glyphWindow.Show();

            var msg_1 = "Hello, world! i0($} ▚ ¾ ∜ -_¬~";
            var msg_2 = "Got to be funky. CQUPOJ8";
            var msg_3 = "0123456789\nBut, in a larger sense, we can not dedicate - we can not consecrate—we can not hallow—this ground. The brave men,\n" +
                        "living and dead, who struggled here, have consecrated it, far above our poor power to add or detract. The world will\n" +
                        "little note, nor long remember what we say here, but it can never forget what they did here.";
            var msg_4 = "ABCDE";
            
            var memBefore = GC.GetTotalMemory(false);

            var sw = new Stopwatch();
            sw.Start();
            
            var prox = new FormsBitmap(img);
            float left = 25;
            float baseline = 140f;
            float scale = 48f / daveFnt.Height();
            float letterSpace = 5;
            
            // Draw first message with angular font
            for (int i = 0; i < msg_1.Length; i++)
            {
                var glyph = daveFnt.ReadGlyph(msg_1[i]);

                Renderers.DrawGlyph(prox, left, baseline, scale, glyph, false);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }

            // Draw second message with curvy font
            left = 25;
            baseline = 200f;
            scale = 0.05f;
            for (int i = 0; i < msg_2.Length; i++)
            {
                var glyph = guthenFnt.ReadGlyph(msg_2[i]);

                Renderers.DrawGlyph(prox, left, baseline, scale, glyph, false);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }
            
            // Draw second message with very curvy font
            left = 25;
            baseline = 280f;
            scale = 0.05f;
            for (int i = 0; i < msg_4.Length; i++)
            {
                var glyph = bendyFnt.ReadGlyph(msg_4[i]);

                Renderers.DrawGlyph(prox, left, baseline, scale, glyph, false);
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

                Renderers.DrawGlyph(prox, left, baseline, scale, glyph, false);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }
            
            
            // Show a sample of the sub-pixel AA strategy
            left = 5;
            baseline = 300f;
            scale = 16f / daveFnt.Height(); // about 11pt
            letterSpace = 2;
            for (int i = 0; i < 58; i++)
            {
                var glyph = daveFnt.ReadGlyph((char) ('A'+i));
                Renderers.RenderSubPixel_RGB_Super3(prox, left, baseline + 20, scale, glyph, false);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }
            
            left = 5;
            baseline = 320f;
            scale = 12f / notoFnt.Height();
            for (int i = 0; i < 58; i++)
            {
                var glyph = notoFnt.ReadGlyph((char) ('A'+i));
                Renderers.RenderSubPixel_RGB_Super3(prox, left, baseline + 20, scale, glyph, false);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }
            left = 5;
            baseline = 340f;
            scale = 10f / notoFnt.Height();
            for (int i = 0; i < 58; i++)
            {
                var glyph = notoFnt.ReadGlyph((char) ('A'+i));
                Renderers.RenderSubPixel_RGB_Super3(prox, left, baseline + 20, scale, glyph, false);
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
                Renderers.RenderSubPixel_RGB_Super3(prox, left, baseline + 35, scale *0.6f, glyph, true);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }
            
            // Varying size
            left = 5;
            baseline = 480f;
            scale = 4f / notoFnt.Height();
            letterSpace = 0.8f;
            var ampGlyph = notoFnt.ReadGlyph('&');//daveFnt.ReadGlyph('Η');
            for (int i = 0; i < 50; i++)
            {
                scale += 0.001f;
                Renderers.DrawGlyph(prox, left, baseline, scale, ampGlyph, true);
                left += (float)ampGlyph.xMax * scale;
                left += letterSpace;
            }
            
            // Large black-on-white
            left = 25;
            baseline = 540f;
            scale = 70f / notoFnt.Height();
            letterSpace = 5;
            for (int i = 0; i < msg_1.Length; i++)
            {
                var glyph = notoFnt.ReadGlyph(msg_1[i]);

                Renderers.DrawGlyph(prox, left, baseline, scale, glyph, true);
                left += (float)glyph.xMax * scale;
                left += letterSpace;
            }

            // Huge curve
            scale = 370f / notoFnt.Height();
            for (int i = 0; i < 1; i++)
            {
                var glyph = notoFnt.ReadGlyph('&'); // Show quality of curve-to-line interpolation
                Renderers.DrawGlyph(prox, 600, 350, scale, glyph, false);
            }
            

            sw.Stop();
            Text = "Glyph find & render took: " + sw.Elapsed;

            // Prove that the render cache works:
            scale = 10f / notoFnt.Height();
            var nullProx = new NullProxy();
            sw.Reset();
            sw.Start();
            for (int i = 0; i < 200_000; i++)
            {
                var glyph = notoFnt.ReadGlyph((char)('a' + (i % 52)));
                Renderers.RenderSubPixel_RGB_Super3(nullProx, 0, 0, scale, glyph, false);
            }
            sw.Stop();
            Text += "; Render stress test: " + sw.Elapsed;

            var memAfter = GC.GetTotalMemory(false);
            
            Text += "; Memory use: " + ((memAfter - memBefore) / 1048576) + "MB";

            outputPictureBox.Image = img;
            Width = img.Width + 18;
            Height = img.Height + 41;
        }
    }

    /// <summary>
    /// An image proxy that does nothing (for testing)
    /// </summary>
    internal class NullProxy : BitmapProxy
    {
        /// <inheritdoc />
        public override void SetPixel(int x, int y, int r, int g, int b)
        {
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
