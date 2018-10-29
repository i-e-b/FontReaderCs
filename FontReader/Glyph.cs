namespace FontReader
{
    public class Glyph {
        /// <summary>
        /// Glyph type. Used to interpret the other parts of a glyph structure
        /// </summary>
        public GlyphTypes GlyphType;

        /// <summary>
        /// Number of connected point sets in the glyph
        /// </summary>
        public int NumberOfContours;

        // Container box size
        public double xMin, xMax, yMin, yMax;

        /// <summary>
        /// Components if this is a compound glyph (made of transformed copies of other glyphs)
        /// Null if this is a simple glyph
        /// </summary>
        public CompoundComponent[] Components;

        public GlyphPoint[] Points;
        public int[] ContourEnds;
    }
    

    public struct CompoundComponent
    {
        public int GlyphIndex;
        public double[] Matrix;  // variable sized
        public int? DestPointIndex;
        public int? SrcPointIndex;
    }

    public struct GlyphPoint
    {
        public bool OnCurve;
        public double X;
        public double Y;
    }
}