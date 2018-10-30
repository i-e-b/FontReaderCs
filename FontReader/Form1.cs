using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

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
            var img = new Bitmap(640, 480, PixelFormat.Format24bppRgb);

            var daveFnt = new TrueTypeFont("dave.ttf"); // a font that uses only straight edges (easy to render)
            var guthenFnt = new TrueTypeFont("guthen_bloots.ttf"); // a very curvy font (control points not yet supported)

            var msg_1 = "Hello, world! $ ▚ ¾ ∜";
            var msg_2 = "Got to be funky";
            float left = 25;
            float baseline = 150f;
            float scale = 0.03f;
            float letterSpace = 5;

            using (var g = Graphics.FromImage(img)) {
                g.SmoothingMode = SmoothingMode.None;

                // Draw first message with angular font
                for (int i = 0; i < msg_1.Length; i++)
                {
                    var glyph = daveFnt.ReadGlyph(msg_1[i]);

                    DrawGlyph(g, left, baseline, scale, glyph);
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

                    DrawGlyph(g, left, baseline, scale, glyph);
                    left += (float)glyph.xMax * scale;
                    left += letterSpace;
                }
            }

            outputPictureBox.Image = img;
            Width = img.Width + 18;
            Height = img.Height + 41;
        }

        private void DrawGlyph(Graphics g, float dx, float dy, float scale, Glyph glyph)
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
                /*if (point.OnCurve) { ... }*/ // to handle control points
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
            // Green = winding increase, Red = Winding decrease. (Blue would be ignored in non-zero winding?)
            if (prev.Y > next.Y) g.DrawLine(Pens.GreenYellow, prev, next);
            else if (prev.Y == next.Y) g.DrawLine(Pens.LightSkyBlue, prev, next);
            else g.DrawLine(Pens.Red, prev, next);
        }
    }
}
