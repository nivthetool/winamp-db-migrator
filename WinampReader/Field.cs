// Field.cs
//
//  Copyright (C) 2009 [name of author]
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace WinampReader
{
    public abstract class Field
    {
        public uint Id { get; private set; }
        public int MaxSizeOnDisk { get; private set; }
        public int NextFieldPos { get; private set; }
        public int PrevFieldPos { get; private set; }
        public FieldType FieldType { get; protected set; }        
        
        protected void ReadBasicProperties(BinaryReader reader)
        {
            Id = reader.ReadByte();
            FieldType = (FieldType) reader.ReadByte();
            MaxSizeOnDisk = reader.ReadInt32();
            NextFieldPos = reader.ReadInt32();
            PrevFieldPos = reader.ReadInt32();
        }

        public static Field GetField(BinaryReader reader, Int32 position)
        {
            reader.BaseStream.Seek(position + sizeof(byte), SeekOrigin.Begin);
            byte b = reader.ReadByte();
            if (b == 2)
                // Special redirection type
                Debugger.Break();
            if (!Enum.IsDefined(typeof(FieldType), b))
                return null;
            Field retval = null;
            FieldType fType = (FieldType)b;
            // Reset the stream to the beginning of this field
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
            switch (fType)
            {
                case FieldType.Column:
                    retval = new ColumnField(reader);
                    break;
                case FieldType.Index:
                    Debugger.Break();
                    break;
                case FieldType.String:
                    retval = new StringField(reader);                    
                    break;
                case FieldType.Integer:
                    retval = new IntegerField(reader);
                    break;
                case FieldType.Datetime:
                    retval = new DatetimeField(reader);
                    break;
                case FieldType.Length:
                    retval = new IntegerField(reader);
                    break;
                case FieldType.Filename:
                    retval = new StringField(reader);
                    break;
                default:
                    Debugger.Break();
                    break;
            }
            return retval;
        }
        public abstract object GetValue();
        public override string ToString()
        {
            return String.Format("{0}", GetValue());
        }
    }

    public class StringField : Field
    {
        public StringField(BinaryReader reader)
        {           
            ReadBasicProperties(reader);
            var strLength = reader.ReadInt16();
            var data = reader.ReadBytes(strLength);

            // For now, assume BOM mark is there and specifies LittleEndian UTF-16 (it is on my machine)
            Value = Encoding.Unicode.GetString(data, 2, data.Length - 2);
        }

        public string Value { get; set; }

        public override object GetValue()
        {
            return Value;
        }
    }

    public class IntegerField : Field
    {
        public IntegerField(BinaryReader reader)
        {
            ReadBasicProperties(reader);
            if (MaxSizeOnDisk != sizeof(int))
                Debugger.Break();
            Value = reader.ReadInt32();
        }
        public int Value { get; set; }
        public override object GetValue()
        {
            return Value;
        }
    }

    public class DatetimeField : Field
    {
        private static DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        public DatetimeField(BinaryReader reader)
        {
            ReadBasicProperties(reader);
            int timestamp = reader.ReadInt32();
            Value = Epoch + TimeSpan.FromSeconds(timestamp);
        }
        public DateTime Value { get; set; }
        public override object GetValue()
        {
            return Value;
        }
    }
    public class ColumnField : Field
    {
        public ColumnField(BinaryReader reader)
        {
            ReadBasicProperties(reader);
            Byte myType = reader.ReadByte();
            bool indexUnique = reader.ReadByte() != 0;
            var strLength = reader.ReadByte();
            var data = reader.ReadBytes(strLength);
            Value = System.Text.Encoding.ASCII.GetString(data);
        }
        
        public string Value { get; private set; }
        
        public override object GetValue()
        {
            return Value;
        }
    }
    public enum FieldType : byte
    {
        Column      = 0,
        Index       = 1,
        String      = 3,
        Integer     = 4,
        Datetime    = 10,
        Length      = 11,
        Filename    = 12,
    }
}
