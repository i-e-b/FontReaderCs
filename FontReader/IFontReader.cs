namespace FontReader
{
    public interface IFontReader
    {
        Glyph ReadGlyph(int index);
    }
}