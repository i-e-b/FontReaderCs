namespace FontReader.Read
{
    public interface IFontReader
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
        /// </summary>
        Glyph ReadGlyph(char c);
    }
}