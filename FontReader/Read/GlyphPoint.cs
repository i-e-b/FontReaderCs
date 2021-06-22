namespace FontReader.Read
{
    public class GlyphPoint
    {
        public bool OnCurve;
        public double X;
        public double Y;

        public GlyphPoint() { }

        public GlyphPoint(double x, double y)
        {
            X=x; Y=y; OnCurve=true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}