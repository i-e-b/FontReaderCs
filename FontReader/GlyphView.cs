using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using FontReader.Read;

namespace FontReader
{
    public partial class GlyphView : Form
    {
        private TrueTypeFont _font;

        public GlyphView()
        {
            InitializeComponent();
            glyphDisplay!.MouseDown += StartMouse;
            glyphDisplay.MouseUp += EndMouse;
            glyphDisplay.MouseMove += DoMouseMove;
            MouseWheel += DoMouseWheel;
            KeyDown += DoKeyDown;
            KeyUp += DoKeyUp;
        }

        double _scale = 0.1;
        double _dx = 125;
        double _dy = 125;
        int _mox, _moy;
        bool _mouseActive;
        private Glyph _glyph;
        
        const double MinScale = 0.000000001;

        private void DoKeyUp(object sender, KeyEventArgs e)
        {
        }

        private void DoKeyDown(object sender, KeyEventArgs e)
        {
        }

        private void DoMouseWheel(object sender, MouseEventArgs e)
        {
            if (e == null) return;
            if (ModifierKeys.HasFlag(Keys.Control)) { _dy += e.Delta * _scale; }
            else if (ModifierKeys.HasFlag(Keys.Alt)) { _dx += e.Delta * _scale; }
            else { 
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
            UpdateGlyphView();
            base.OnPaint(e);
        }

        public void SetFont(TrueTypeFont font)
        {
            _font = font;
        }

        private void pickButton_Click(object sender, EventArgs e)
        {
            var chr = characterBox?.Text?.FirstOrDefault() ?? '\0';
            _glyph = _font?.ReadGlyph(chr); // TODO: chars other than the 1st shown as onion layers; Handle \u0000 format chars

            Invalidate();
        }

        private void UpdateGlyphView()
        {
            if (_glyph == null || glyphDisplay == null) return;
            
            var img = new Bitmap(glyphDisplay.Width, glyphDisplay.Height);
            glyphDisplay.Image?.Dispose();
            
            var background = Color.White;
            var curvePoint = Pens.Black;
            var controlPoint = Pens.Brown;
            var curveLine = Pens.Goldenrod;
            var guide = Pens.Chartreuse;
            
            using (var g = Graphics.FromImage(img))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                
                g.Clear(background);
                
                g.DrawLine(guide, (float)_dx, 0, (float)_dx, img.Height); // X=0 line
                g.DrawLine(guide, 0, (float)_dy, img.Width, (float)_dy); // Y=0 line
                
                var pts = _glyph.Points;
                if (pts == null) return;
                foreach (var point in pts)
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
                
                var curves = _glyph.NormalisedContours();
                foreach (var curve in curves)
                {
                    g.DrawLines(curveLine, curve.Select(f => 
                        new PointF(
                            (float) (_dx + ((f.X + _glyph.xMin) * _scale)),
                            (float) (_dy + ((-f.Y - _glyph.yMin) * _scale))
                        )).ToArray());
                }
            }

            glyphDisplay.Image = img;
        }
    }
}