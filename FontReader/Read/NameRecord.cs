﻿namespace FontReader.Read
{
    public class NameRecord
    {
        public NameRecord(BinaryReader file, long tableBase, long stringOffset)
        {
            PlatformId = file.GetUint16();
            EncodingId = file.GetUint16();
            LanguageId = file.GetUint16();
            NameId = file.GetUint16();
            Length = file.GetUint16();
            Offset = file.GetUint16();
            
            if (Length > 255) return; // safety valve
            
            file.PushPosition();
            
            file.Seek(Offset + tableBase + stringOffset);
            StringValue = file.GetString(Length);
            
            file.PopPosition();
        }

        public string StringValue { get; set; }

        public ushort Offset { get; set; }

        public ushort Length { get; set; }

        public ushort NameId { get; set; }

        public ushort LanguageId { get; set; }

        public ushort EncodingId { get; set; }

        public ushort PlatformId { get; set; }
    }
}