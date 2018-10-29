using System;
using System.Collections.Generic;

namespace FontReader
{
    public class TrueTypeFont : IFontReader
    {
        public const uint HEADER_MAGIC = 0x5f0f3cf5;

        private readonly BinaryReader file;
        private readonly Dictionary<string, OffsetEntry> _tables;

        private readonly FontHeader _header;

        private uint _scalarType;
        private ushort _searchRange;
        private ushort _entrySelector;
        private ushort _rangeShift;
        private int _length;

        public TrueTypeFont(string filename)
        {
            file = new BinaryReader(filename);

            // The order that things are read below is important
            // DO NOT REARRANGE CALLS!
            _tables = ReadOffsetTables();
            _header = ReadHeadTable();
            _length = GlyphCount();

            if ( ! _tables.ContainsKey("glyf")) throw new Exception("Bad font: glyf table missing");
            if ( ! _tables.ContainsKey("loca")) throw new Exception("Bad font: loca table missing");
        }

        private int GlyphCount()
        {
            if ( ! _tables.ContainsKey("maxp")) throw new Exception("Bad font: maxp table missing (no glyph count)");
            var old = file.Seek(_tables["maxp"].Offset + 4);
            var count = file.GetUint16();
            file.Seek(old);
            return count;
        }

        private FontHeader ReadHeadTable()
        {
            if ( ! _tables.ContainsKey("head")) throw new Exception("Bad font: Header table missing");
            file.Seek(_tables["head"].Offset);

            var h = new FontHeader();

            h.Version = file.GetFixed();
            h.Revision = file.GetFixed();
            h.ChecksumAdjustment = file.GetUint32();
            h.MagicNumber = file.GetUint32();

            if (h.MagicNumber != HEADER_MAGIC) throw new Exception("Bad font: incorrect identifier in header table");

            h.Flags = file.GetUint16();
            h.UnitsPerEm = file.GetUint16();
            h.Created = file.GetDate();
            h.Modified = file.GetDate();

            h.xMin = file.GetFWord();
            h.yMin = file.GetFWord();
            h.xMax = file.GetFWord();
            h.yMax = file.GetFWord();

            h.MacStyle = file.GetUint16();
            h.LowestRecPPEM = file.GetUint16();
            h.FontDirectionHint = file.GetInt16();
            h.IndexToLocFormat = file.GetInt16();
            h.GlyphDataFormat = file.GetInt16();

            return h;
        }

        public Dictionary<string, OffsetEntry> ReadOffsetTables()
        {
            var tables = new Dictionary<string, OffsetEntry>();

            // DO NOT REARRANGE CALLS!
            _scalarType = file.GetUint32();
            var numTables = file.GetUint16();

            _searchRange = file.GetUint16();
            _entrySelector = file.GetUint16();
            _rangeShift = file.GetUint16();

            for (int i = 0; i < numTables; i++)
            {
                var tag = file.GetString(4);
                var entry = new OffsetEntry{
                    Checksum = file.GetUint32(),
                    Offset = file.GetUint32(),
                    Length = file.GetUint32()
                };
                tables.Add(tag, entry);

                if (tag != "head") {
                    if (CalculateTableChecksum(file, tables[tag].Offset, tables[tag].Length) != tables[tag].Checksum)
                        throw new Exception("Bad file format: checksum fail in offset tables");
                }
            }
            return tables;
        }

        private long CalculateTableChecksum(BinaryReader reader, uint offset, uint length)
        {
            var old = reader.Seek(offset);
            long sum = 0;
            var nlongs = (length + 3) / 4;
            while( nlongs > 0 ) {
                nlongs--;
                sum += reader.GetUint32() & 0xFFFFFFFFu;
            }

            file.Seek(old);
            return sum;
        }

        public Glyph ReadGlyph(int index)
        {
            var offset = GetGlyphOffset(index);
            
            if (offset >= _tables["glyf"].Offset + _tables["glyf"].Length) throw new Exception("Bad font: Invalid glyph offset (too high)");
            if (offset < _tables["glyf"].Offset) throw new Exception("Bad font: Invalid glyph offset (too low)");


        }

        private long GetGlyphOffset(int index)
        {
            var table = _tables["loca"];
            var size = table.Offset + table.Length;
            long offset, old;

            if (_header.IndexToLocFormat == 1) {
                var target = table.Offset + index * 4;
                if (target + 4 > size) throw new Exception("Glyph index out of range");
                old = file.Seek(target);
                offset = file.GetUint32();
            } else {
                var target = table.Offset + index * 2;
                if (target + 2 > size) throw new Exception("Glyph index out of range");
                old = file.Seek(target);
                offset = file.GetUint16() * 2;
            }

            file.Seek(old);
            return offset + _tables["glyf"].Offset;
        }
    }
}