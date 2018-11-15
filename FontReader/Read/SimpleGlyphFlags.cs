using System;

namespace FontReader.Read
{
    [Flags]
    public enum SimpleGlyphFlags : byte
    {
        ON_CURVE =  1,
        X_IS_BYTE       =  2,
        Y_IS_BYTE       =  4,
        REPEAT          =  8,
        X_DELTA         = 16,
        Y_DELTA         = 32
    }
}