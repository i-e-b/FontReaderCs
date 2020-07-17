using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FontReader.Read;

namespace FontReader
{
    /// <summary>
    /// A view and edit screen for glyph forms
    /// </summary>
    /// <remarks>
    /// Plan:
    /// Movement
    ///  * click/drag without key is always pan
    ///  * wheel without key is always scale
    ///  * ALT-click to move a point at high precision, without grid
    ///  * CTRL-click to move a point on grid
    ///  * SHIFT-click to select points on glyph without moving
    ///    * SHIFT-CTRL-click to select points on onion layers without moving
    ///
    /// Grid and onion
    ///  * Can set grid size and origin by exact number
    ///  * Can set grid size and origin by point selection
    ///  * multiple onion layers can be shown, all greyed out (translucent)
    ///
    /// Data
    ///  * Subclass of Glyph that doesn't cache contour forms
    ///  * Can save out to a folder
    ///  * Due to references, will need to have a separate build/export phase
    /// </remarks>
    public sealed partial class GlyphView : Form
    {
        private TrueTypeFont _font;

        public GlyphView()
        {
            InitializeComponent();
            MouseDown += StartMouse;
            MouseUp += EndMouse;
            MouseMove += DoMouseMove;
            MouseWheel += DoMouseWheel;
            KeyDown += DoKeyDown;
            KeyUp += DoKeyUp;
            
            DoubleBuffered = true;
        }

        double _scale = 0.1;
        double _dx = 125;
        double _dy = 125;
        int _mox, _moy;
        bool _mouseActive;
        private EditGlyph _glyph;

        const double MinScale = 0.05;

        private void DoKeyUp(object sender, KeyEventArgs e)
        {
        }

        private void DoKeyDown(object sender, KeyEventArgs e)
        {
        }

        private void DoMouseWheel(object sender, MouseEventArgs e)
        {
            if (e == null) return;
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                _dy += e.Delta * _scale;
            }
            else if (ModifierKeys.HasFlag(Keys.Alt))
            {
                _dx += e.Delta * _scale;
            }
            else
            {
                _scale += e.Delta * 0.0001;
                if (_scale < MinScale) _scale = MinScale;
            }

            Invalidate();
        }

        private void EndMouse(object sender, MouseEventArgs e)
        {
            _mouseActive = false;
        }

        private void StartMouse(object sender, MouseEventArgs e)
        {
            if (e == null) return;
            _mox = e.X;
            _moy = e.Y;
            _mouseActive = true;
        }

        private void DoMouseMove(object sender, MouseEventArgs e)
        {
            if (e == null || !_mouseActive) return;
            _dx += e.X - _mox;
            _dy += e.Y - _moy;
            _mox = e.X;
            _moy = e.Y;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UpdateGlyphView(e.Graphics);
        }

        public void SetFont(TrueTypeFont font)
        {
            _font = font;
        }

        private string NormaliseText(string str)
        {
            if (str == null) return "";

            var sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\\')
                {
                    // escape code
                    if ((i + 5) >= str.Length)
                    {
                        sb.Append(str[i]);
                        continue;
                    }

                    if (char.ToLower(str[i + 1]) != 'u')
                    {
                        sb.Append(str[i]);
                        continue;
                    }

                    var hex = str.Substring(i + 2, 4);
                    var che = char.ConvertFromUtf32(int.Parse(hex, NumberStyles.HexNumber));
                    sb.Append(che);
                }
                else sb.Append(str[i]);
            }

            return sb.ToString();
        }

        private void UpdateGlyphView(Graphics g)
        {
            if (g == null) return;
            if (_glyph == null) return;
            
            var background = Color.White;
            var curvePoint = Pens.Black;
            var controlPoint = Pens.MediumVioletRed;
            var curveLine = Pens.Goldenrod;
            var majorGuide = Pens.Chartreuse;
            var minorGuide = Pens.Beige;

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            g.Clear(background);

            var gridStep = _scale * 150;
            for (double gi = _dx; gi < Width; gi += gridStep) { g.DrawLine(minorGuide, (float) gi, 0, (float) gi, Height); }
            for (double gi = _dx; gi >= 0; gi -= gridStep) { g.DrawLine(minorGuide, (float) gi, 0, (float) gi, Height); }

            for (double gi = _dy; gi < Height; gi += gridStep) { g.DrawLine(minorGuide, 0, (float) gi, Width, (float) gi); }
            for (double gi = _dy; gi >= 0; gi -= gridStep) { g.DrawLine(minorGuide, 0, (float) gi, Width, (float) gi); }

            g.DrawLine(majorGuide, (float) _dx, 0, (float) _dx, Height); // X=0 line
            g.DrawLine(majorGuide, 0, (float) _dy, Width, (float) _dy); // Y=0 line

            var contours = _glyph.Curves;

            foreach (var contour in contours)
            {

                foreach (var point in contour.Points)
                {
                    if (point == null) continue;
                    var x = _dx + (point.X * _scale);
                    var y = _dy + (-point.Y * _scale);
                    if (point.OnCurve)
                    {
                        g.DrawRectangle(curvePoint, (float) x - 2, (float) y - 2, 4, 4);
                    }
                    else
                    {
                        g.DrawEllipse(controlPoint, (float) x - 2, (float) y - 2, 4, 4);
                    }
                }

                var curve = contour.Render();
                g.DrawLines(curveLine, curve!.Select(f =>
                    new PointF(
                        (float) (_dx + (f!.X * _scale)),
                        (float) (_dy + (-f.Y * _scale))
                    )).ToArray());
            }
        }

        private void characterBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e?.KeyCode != Keys.Enter && e?.KeyCode != Keys.Return) return;
            
            var normal = NormaliseText(characterBox?.Text);
            var chr = normal?.FirstOrDefault() ?? '\0';
            _glyph = new EditGlyph(_font?.ReadGlyph(chr)); // TODO: chars other than the 1st shown as onion layers; Handle \u0000 format chars

            Invalidate();
        }
    }
}