using System;
using System.IO;
using System.Text;

// http://stevehanov.ca/blog/TrueType.js
namespace FontReader
{
    /// <summary>
    /// Byte reader for TTF / OpenType files
    /// </summary>
    public class BinaryReader {
        private readonly byte[] data;
        private long pos;

        /// <summary>
        /// Load from a file
        /// </summary>
        public BinaryReader(string filename)
        {
            data = File.ReadAllBytes(filename);
            if (data == null) throw new Exception("Failed to read file");

            pos = 0;
        }

        /// <summary>
        /// Seek to absolute position. Returns previous position
        /// </summary>
        public long Seek(long newPos)
        {
            var oldPos = pos;
            pos = newPos;
            return oldPos;
        }

        /// <summary>
        /// Return current position
        /// </summary>
        public long Tell() => pos;

        /// <summary> Read an unsigned byte from current position and advance </summary>
        public byte GetUint8(){ return data[pos++]; } 
        
        /// <summary> Read an unsigned 16bit word from current position and advance </summary>
        public ushort GetUint16(){ return (ushort) ((GetUint8() << 8) | (GetUint8())); }

        /// <summary> Read an unsigned 16bit word from current position and advance </summary>
        public uint GetUint32() { return (uint)GetInt32(); }
        
        /// <summary> Read an signed 16bit word from current position and advance </summary>
        public short GetInt16() {
            var result = GetUint16();
            if ((result & 0x8000) > 0) {
                var adj = result - (1 << 16);
                return (short)adj;
            }
            return (short)result;
        }

        /// <summary> Read an signed 32bit word from current position and advance </summary>
        public int GetInt32()
        {
            return (GetUint8() << 24) |
                   (GetUint8() << 16) |
                   (GetUint8() << 8) |
                   (GetUint8());
        }

        /// <summary>
        /// Get signed fixword size
        /// </summary>
        public short GetFWord() { return GetInt16(); }

        /// <summary> Get float from a 16 bit fixed point </summary>
        public double Get2Dot14() {
            return (double)GetInt16() / (1 << 14);
        }

        /// <summary> Get float from 32 bit fixed point </summary>
        public double GetFixed() {
            return (double)GetInt32() / (1 << 16);
        }

        /// <summary>
        /// Read an ASCII string from current position and advance
        /// </summary>
        public string GetString(int length) {
            var result = new StringBuilder();
            for(var i = 0; i < length; i++) {
                result.Append( (char)GetUint8() );
            }
            return result.ToString();
        }

        /// <summary> Read a TTF / Opentype date </summary>
        public DateTime GetDate() {
            var macTimeSeconds = GetUint32() * 0x100000000UL + GetUint32();
            var utcBase = new DateTime(1904, 1, 1, 0, 0, 0);

            return utcBase.AddSeconds(macTimeSeconds);
        }
    }
}