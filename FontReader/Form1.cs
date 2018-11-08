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

            var msg_1 = "Hello, world! $ ▚ ¾ ∜ -_¬~";
            var msg_2 = "Got to be funky";
            var msg_3 = "But, in a larger sense, we can not dedicate—we can not consecrate—we can not hallow—this ground. The brave men, living and dead,\n" +
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
            scale = 0.01f;
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
            var bmp = NonZeroWindingDraw.Render(glyph, scale);
            var c = Color.White;

            for (int y = 0; y < bmp.GetLength(0); y++)
            {
                for (int x = 0; x < bmp.GetLength(1); x++)
                {
                    var v = bmp[y,x];
                    if (v == 0) continue;

                    var r = 0;
                    var g = 0;
                    var b = 0;

                    if ((v & NonZeroWindingDraw.WIND_DOWN) > 0) r += 125;
                    if ((v & NonZeroWindingDraw.WIND_UP) > 0) g += 125;
                    //if ((v & NonZeroWindingDraw.WIND_LEFT) > 0) b += 125;
                    //if ((v & NonZeroWindingDraw.WIND_RITE) > 0) b += 125;
                    //if ((v & NonZeroWindingDraw.TOUCHED) > 0) b += 125;
                    if ((v & NonZeroWindingDraw.INSIDE) > 0) b = 255;

                    img.SetPixel((int)dx+x, (int)dy-y, Color.FromArgb(r, g, b));
                }
            }
        }

        private void DrawGlyphOld(Graphics g, float dx, float dy, float scale, Glyph glyph)
        {
            if (glyph?.GlyphType != GlyphTypes.Simple) return;

            var p = 0;
            var c = 0;
            var first = 1;
            var close = new PointF();
            var prev = new PointF();
            var next = new PointF();

            while (p < glyph.Points.Length) {
                var point = glyph.Points[p];
                /*if (!point.OnCurve) { ... }*/ // to handle control points
                prev = next;
                next = new PointF((float) (dx + point.X * scale), (float) (dy - point.Y * scale)); // can adjust the X scale here to help with sub-pixel AA

                if (first == 1) {
                    close = next;
                    first = 0;
                } else
                {
                    // currently totally ignores control points and curves
                    ColorCodedLine(g, prev, next);
                }

                if (p == glyph.ContourEnds[c]) {
                    ColorCodedLine(g, next, close); // ensure closed paths
                    c++;
                    first = 1;
                }
                
                p++;
            }
        }

        private static void ColorCodedLine(Graphics g, PointF prev, PointF next)
        {
            g.DrawLine(ThinWhite, prev, next);
           /* if (prev.Y > next.Y) g.DrawLine(Pens.Yellow, prev, next);
            else if (prev.Y == next.Y) g.DrawLine(Pens.White, prev, next);
            else g.DrawLine(Pens.Cyan, prev, next);*/
        }
    }
}
