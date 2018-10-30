namespace FontReader
{
    public interface IFontReader
    {
        Glyph ReadGlyphByIndex(int index, bool forceEmpty);

        /// <summary>
        /// https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
        /// </summary>
        Glyph ReadGlyph(char c);
    }
}