using System;
using System.Collections.Generic;
using System.Linq;

namespace FontReader.Read
{
    public class TrueTypeFont : IFontReader
    {
        public const uint HEADER_MAGIC = 0x5f0f3cf5;

        private readonly BinaryReader file;
        private readonly Dictionary<string, OffsetEntry> _tables;
        private readonly Dictionary<char, int> _unicodeIndexes;

        private readonly FontHeader _header;

        private uint _scalarType;
        private ushort _searchRange;
        private ushort _entrySelector;
        private ushort _rangeShift;
        private int _length;

        
        /// <summary>
        /// Get the reported overall height of the font
        /// </summary>
        public float Height()
        {
            return (float)_header.xMax - _header.xMin;
        }
        
        public Glyph ReadGlyphByIndex(int index, bool forceEmpty)
        {
            var offset = GetGlyphOffset(index);

            if (offset >= _tables["glyf"].Offset + _tables["glyf"].Length) throw new Exception("Bad font: Invalid glyph offset (too high) at index " + index);
            if (offset < _tables["glyf"].Offset) throw new Exception("Bad font: Invalid glyph offset (too low) at index" + index);

            file.Seek(offset);
            var glyph = new Glyph{
                NumberOfContours = file.GetInt16(),
                xMin = file.GetFWord(),
                yMin = file.GetFWord(),
                xMax = file.GetFWord(),
                yMax = file.GetFWord()
            };

            if (glyph.NumberOfContours < -1) throw new Exception("Bad font: Invalid contour count at index " + index);

            var baseOffset = file.Position();
            if (forceEmpty || glyph.NumberOfContours == 0) return EmptyGlyph(glyph);
            if (glyph.NumberOfContours == -1) return ReadCompoundGlyph(glyph, baseOffset);
            return ReadSimpleGlyph(glyph);
        }
        
        public Glyph ReadGlyph(char wantedChar)
        {
            if ( ! _unicodeIndexes.ContainsKey(wantedChar)) {
                _unicodeIndexes.Add(wantedChar,  GlyphIndexForChar(wantedChar));
            }

            return ReadGlyphByIndex(_unicodeIndexes[wantedChar], char.IsWhiteSpace(wantedChar));
        }

        private int GlyphIndexForChar(char wantedChar)
        {
            // read cmap table if possible,
            // then call down to the index based ReadGlyph.

            if ( ! _tables.ContainsKey("cmap")) throw new Exception("Can't translate character: cmap table missing");
            file.Seek(_tables["cmap"].Offset);

            var vers = file.GetUint16();
            var numTables = file.GetUint16();
            var offset = 0u;
            var found = false;

            for (int i = 0; i < numTables; i++)
            {
                var platform = file.GetUint16();
                var encoding = file.GetUint16();
                offset = file.GetUint32();

                if (platform == 3 && encoding == 1) // Unicode 2 byte encoding for Basic Multilingual Plane
                {
                    found = true;
                    break;
                }
            }

            if (!found) {
                return 0; // the specific 'missing' glyph
            }
            
            // format 4 table
            if (offset < file.Position()) {file.Seek(_tables["cmap"].Offset + offset); } // guessing
            else { file.Seek(offset); }

            var subtableFmt = file.GetUint16();

            var byteLength = file.GetUint16();
            var res1 = file.GetUint16(); // should be 0
            var segCountX2 = file.GetUint16();
            var searchRange = file.GetUint16();
            var entrySelector = file.GetUint16();
            var rangeShift = file.GetUint16();

            if (subtableFmt != 4) throw new Exception("Invalid font: Unicode BMP table with non- format 4 subtable");
            
            // read the parallel arrays
            var segs = segCountX2 / 2;
            var endsBase = file.Position();
            var startsBase = endsBase + segCountX2 + 2;
            var idDeltaBase = startsBase + segCountX2;
            var idRangeOffsetBase = idDeltaBase + segCountX2;

            var targetSegment = -1;

            var c = (int)wantedChar;

            for (int i = 0; i < segs; i++)
            {
                int end = file.PickUint16(endsBase, i);
                int start = file.PickUint16(startsBase, i);
                if (end >= c && start <= c) {
                    targetSegment = i;
                    break;
                }
            }
            
            if (targetSegment < 0) return 0; // the specific 'missing' glyph

            var rangeOffset = file.PickUint16(idRangeOffsetBase, targetSegment);
            if (rangeOffset == 0) {
                // direct lookup:
                var lu = file.PickInt16(idDeltaBase, targetSegment); // this can represent a negative by way of the modulo
                var glyphIdx = (lu + c) % 65536;
                return glyphIdx;
            }

            // Complex case. The TrueType spec expects us to have mapped the font into memory, then do some
            // nasty pointer arithmetic. "This obscure indexing trick works because glyphIdArray immediately follows idRangeOffset in the font file"
            //
            // https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
            // https://github.com/LayoutFarm/Typography/wiki/How-To-Translate-Unicode-Character-Codes-to-TrueType-Glyph-Indices-in-Windows-95
            // https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6cmap.html

            var ros = file.PickUint16(idRangeOffsetBase, targetSegment);
            var startc = file.PickUint16(startsBase, targetSegment);
            var offsro = idRangeOffsetBase + (targetSegment * 2);
            var glyphIndexAddress = ros + 2 * (c - startc) + offsro;
            var res = file.PickInt16(glyphIndexAddress, 0);

            return res;
        }


        public TrueTypeFont(string filename)
        {
            file = new BinaryReader(filename);
            _unicodeIndexes = new Dictionary<char, int>();

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
            
            // DO NOT REARRANGE CALLS!
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

        private Dictionary<string, OffsetEntry> ReadOffsetTables()
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

               /* if (tag != "head") {
                    if (CalculateTableChecksum(file, tables[tag].Offset, tables[tag].Length) != tables[tag].Checksum)
                        throw new Exception("Bad file format: checksum fail in offset tables");
                }*/
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

        private Glyph ReadCompoundGlyph(Glyph glyph, long baseOffset)
        {
            // http://stevehanov.ca/blog/TrueType.js
            throw new Exception("Compounds not yet supported");
            // A compound glyph brings together simple glphys from elsewhere in the font
            // and combines them with transforms.
            // If this method gets implemented, it should reduce the components down to a simple glyph
        }

        private Glyph EmptyGlyph(Glyph g)
        {
            g.GlyphType = GlyphTypes.Empty;
            return g;
        }

        private Glyph ReadSimpleGlyph(Glyph g)
        {
            g.GlyphType = GlyphTypes.Simple;

            var ends = new List<int>();

            for (int i = 0; i < g.NumberOfContours; i++) { ends.Add(file.GetUint16()); }

            // Skip past hinting instructions
            file.Skip(file.GetUint16());

            var numPoints = ends.Max() + 1;

            // Flags and points match up
            var flags = new List<SimpleGlyphFlags>();
            var points = new List<GlyphPoint>();

            // Read point flags, creating base entries
            for (int i = 0; i < numPoints; i++)
            {
                var flag = (SimpleGlyphFlags)file.GetUint8();
                flags.Add(flag);
                points.Add(new GlyphPoint{ OnCurve = flag.HasFlag(SimpleGlyphFlags.ON_CURVE)});

                if (flag.HasFlag(SimpleGlyphFlags.REPEAT)) {
                    var repeatCount = file.GetUint8();
                    i += repeatCount;
                    while (repeatCount-- > 0) {
                        flags.Add(flag);
                        points.Add(new GlyphPoint{ OnCurve = flag.HasFlag(SimpleGlyphFlags.ON_CURVE)});
                    }
                }
            }

            // Fill out point data
            ElaborateCoords(flags, points, (p,v) => p.X = v, SimpleGlyphFlags.X_IS_BYTE, SimpleGlyphFlags.X_DELTA, g.xMin, g.xMax);
            ElaborateCoords(flags, points, (p,v) => p.Y = v, SimpleGlyphFlags.Y_IS_BYTE, SimpleGlyphFlags.Y_DELTA, g.yMin, g.yMax);

            g.Points = points.ToArray();
            g.ContourEnds = ends.ToArray();

            // TODO: expand glyph curves into paths?
            // Maybe have another level after finding glyphs, and before rendering

            return g;
        }

        private void ElaborateCoords(List<SimpleGlyphFlags> flags, List<GlyphPoint> points, Action<GlyphPoint, double> map, SimpleGlyphFlags byteFlag, SimpleGlyphFlags deltaFlag, double min, double max)
        {
            var value = 0.0d;

            for (int i = 0; i < points.Count; i++)
            {
                var flag = flags[i];
                if (flag.HasFlag(byteFlag)) {
                    if (flag.HasFlag(deltaFlag)) {
                        value += file.GetUint8();
                    } else {
                        value -= file.GetUint8();
                    }
                } else if (!flag.HasFlag(deltaFlag)) {
                    value += file.GetInt16();
                } else {
                    // value not changed
                    // this is why X and Y are separate
                }

                map(points[i], value);
            }
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