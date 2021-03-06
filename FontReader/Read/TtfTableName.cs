﻿using System.Collections.Generic;

namespace FontReader.Read
{
    public class TtfTableName
    {
        public TtfTableName(BinaryReader file, OffsetEntry table)
        {
            file.Seek(table.Offset);
            TableBase = table.Offset;
            
            // See https://docs.microsoft.com/en-gb/typography/opentype/spec/name
            Format = file.GetUint16();

            switch (Format)
            {
                case 0:
                    ReadFormatZero(file);
                    break;
            }
        }


        private void ReadFormatZero(BinaryReader file)
        {
            Count = file.GetUint16();
            StringOffset = file.GetUint16();
            
            if (Count < 1 || Count > 50) return; // safety valve

            Names = new List<NameRecord>();
            for (int i = 0; i < Count; i++)
            {
                Names.Add(new NameRecord(file, TableBase, StringOffset));
            }
        }

        public List<NameRecord> Names { get; set; }

        public long TableBase { get; set; }
        public long StringOffset { get; set; }
        public ushort Count { get; set; }
        public int Format { get; set; }
    }
}