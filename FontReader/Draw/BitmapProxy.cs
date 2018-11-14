namespace FontReader.Draw
{
    /// <summary>
    /// Abstraction for editable bitmap images
    /// </summary>
    public abstract class BitmapProxy {
        public abstract void SetPixel(int x, int y, int r, int g, int b);
    }
}