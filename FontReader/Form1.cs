using System.Drawing;
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

            /*for (int y = 0; y < 480; y++)
            {
                for (int x = 0; x < 640; x++)
                {
                    img.SetPixel(x, y, Color.FromArgb(x % 128, y % 255, 128 ));
                }
            }*/

            var fr = new TrueTypeFont("dave.ttf");

            //var msg = "Hello, world!";
            var msg = " ";

            using (var g = Graphics.FromImage(img)) {
                for (int i = 0; i < msg.Length; i++)
                {
                    DrawGlyph(g, 25 * i, 200, 0.05f, fr.ReadGlyph(msg[i]));
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
                prev = next;
                next = new PointF((float) (dx + point.X * scale), (float) (dy - point.Y * scale));

                if (first == 1) {
                    close = next;
                    first = 0;
                } else {
                    g.DrawLine(Pens.White, prev, next);
                }

                if (p == glyph.ContourEnds[c]) {
                    g.DrawLine(Pens.White, next, close); // ensure closed paths
                    c++;
                    first = 1;
                }
                
                p++;
            }
        }
    }
}
