using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FontReader.Read;
using JetBrains.Annotations;

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
        private EditGlyph _glyph;

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
        int _cursorX, _cursorY;
        
        int _lastContourIndex = -1;
        int _lastPointIndex = -1;
        bool _pointLock;

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
            _pointLock = false;
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
            if (e == null) return;
            if (_mouseActive) // handle movement
            {
                if (ModifierKeys.HasFlag(Keys.Alt))
                {
                    TryMovePoint(e);
                }
                else
                {
                    PanCanvas(e);
                }
            }

            _cursorX = e.X;
            _cursorY = e.Y;
            Invalidate();
        }

        private void TryMovePoint([NotNull]MouseEventArgs e)
        {
            if (_lastContourIndex < 0 || _lastPointIndex < 0) return; // nothing selected
            if (_glyph == null) return; // no character loaded
            
            var dx = e.X - _cursorX;
            var dy = e.Y - _cursorY;
            
            var point = _glyph.Curves[_lastContourIndex].Points[_lastPointIndex];
            if (point == null) return;
            
            _pointLock = true; // prevent switching point during a drag
            point.X += dx / _scale;
            point.Y -= dy / _scale;
            Invalidate();
        }

        private void PanCanvas([NotNull]MouseEventArgs e)
        {
            _dx += e.X - _mox;
            _dy += e.Y - _moy;
            _mox = e.X;
            _moy = e.Y;
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

            // Highlight nearest point
            GlyphPoint nearestToCursor = null;
            double distanceOfNearest = double.MaxValue;
            

            var contours = _glyph.Curves;
            for (var contourIndex = 0; contourIndex < contours.Count; contourIndex++)
            {
                var contour = contours[contourIndex];
                for (var pointIndex = 0; pointIndex < contour.Points.Count; pointIndex++)
                {
                    var point = contour.Points[pointIndex];
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

                    if (!_pointLock)
                    {
                        if (nearestToCursor == null) nearestToCursor = point;
                        var distToCursor = Math.Sqrt(sqr(x - _cursorX) + sqr(y - _cursorY));
                        if (distToCursor < distanceOfNearest)
                        {
                            _lastContourIndex = contourIndex;
                            _lastPointIndex = pointIndex;
                            nearestToCursor = point;
                            distanceOfNearest = distToCursor;
                        }
                    }
                }

                var curve = contour.Render();
                g.DrawLines(curveLine, curve!.Select(f =>
                    new PointF(
                        (float) (_dx + (f!.X * _scale)),
                        (float) (_dy + (-f.Y * _scale))
                    )).ToArray());
            }

            // highlight nearest point
            if (nearestToCursor != null)
            {
                var x = _dx + (nearestToCursor.X * _scale);
                var y = _dy + (-nearestToCursor.Y * _scale);
                if (nearestToCursor.OnCurve) g.DrawRectangle(curvePoint, (float) x - 4, (float) y - 4, 8, 8);
                else g.DrawEllipse(controlPoint, (float)x - 4, (float)y - 4, 8, 8);
            }
        }

        // ReSharper disable once InconsistentNaming
        private static double sqr(double d)
        {
            return d*d;
        }

        private void characterBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e?.KeyCode == Keys.Enter || e?.KeyCode == Keys.Return) // update character selection
            {
                var normal = NormaliseText(characterBox?.Text);
                var chr = normal?.FirstOrDefault() ?? '\0';

                _lastContourIndex = -1;
                _lastPointIndex = -1;
                _glyph = new EditGlyph(_font?.ReadGlyph(chr)); // TODO: chars other than the 1st shown as onion layers;
            }

            Invalidate();
        }

        private void collapseButton_Click(object sender, EventArgs e)
        {
            // attract control points to their nearest neighbors.
            if (_glyph == null) return;
            var allPoints = new List<GlyphPoint>(_glyph.Curves.SelectMany(c => c.Points)
                .Where(p=>p.OnCurve) // curves are weightless
            );

            // Really inefficient, but simple n*m gravity
            foreach (var contour in _glyph.Curves)
            {
                foreach (var point in contour.Points)
                {
                    //if (!point.OnCurve) continue; // don't move curve points
                    var forces = allPoints.Select(other=>GetForce(point, other)).OrderByDescending(p=>p.M).Take(3).ToList();
                    var sumX = forces.Sum(p=>p.X);
                    var sumY = forces.Sum(p=>p.Y);
                    
                    point.X += sumX;
                    point.Y += sumY;
                }
            }
            Invalidate();
        }

        private GravityPoint GetForce(GlyphPoint self, GlyphPoint other)
        {
            const double scale = 10.0;
            const double radiusLimit = 250.0;
            const double stickyRadius = 5.0;
            
            var dx = Math.Abs(self!.X - other!.X);
            var dy = Math.Abs(self!.Y - other!.Y);
            
            if (dx < 0.0001 && dy < 0.0001) return new GravityPoint {X = 0, Y = 0, M=0};
            
            var dist = Math.Sqrt(dx*dx + dy*dy);
            if (dist > radiusLimit) return new GravityPoint {X = 0, Y = 0, M=0};
            if (dist < stickyRadius) return new GravityPoint {X = 0, Y = 0, M=0};
            
            var f = scale / dist;
            
            var fx = (other.X - self.X) * f;
            var fy = (other.Y - self.Y) * f;

            return new GravityPoint {X = fx, Y = fy, M=f};
        }
    }

    internal class GravityPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double M { get; set; }
    }
}