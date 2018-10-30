namespace FontReader
{
    public interface IFontReader
    {
        Glyph ReadGlyphByIndex(int index);

        /// <summary>
        /// https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
        /// </summary>
        Glyph ReadGlyph(char c);
    }
}