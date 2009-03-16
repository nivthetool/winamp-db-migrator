// Field.cs
//
//  Copyright (C) 2009 Isak Savo <isak.savo@gmail.com>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace WinampReader
{
	/// <summary>
	/// Base class for all fields in a record. A field is a single metadata, such as "artist", "album" or "title".
	/// </summary>
    public abstract class Field
    {
		/// <value>
		/// Gets the ID of this field
		/// </value>
        public uint Id { get; private set; }
		/// <value>
		/// Gets the max size this field can occupy on disk
		/// </value>
        public int MaxSizeOnDisk { get; private set; }
		/// <value>
		/// Gets the location of the next field in this record.
		/// </value>
        public int NextFieldPos { get; private set; }
		/// <value>
		/// Gets the location of the previous field in this record.
		/// </value>
        public int PrevFieldPos { get; private set; }
		/// <value>
		/// Gets the type of the field (string, int, filename, etc.)
		/// </value>
        public FieldType FieldType { get; protected set; }        
        
		/// <summary>
		/// Reads the basic (common) properties from the table
		/// </summary>
		/// <param name="reader">
		/// A <see cref="BinaryReader"/> that will be used to read data. The reader should already be positioned at the correct location.
		/// </param>
        protected void ReadBasicProperties(BinaryReader reader)
        {
            Id = reader.ReadByte();
            FieldType = (FieldType) reader.ReadByte();
            MaxSizeOnDisk = reader.ReadInt32();
            NextFieldPos = reader.ReadInt32();
            PrevFieldPos = reader.ReadInt32();
        }

		/// <summary>
		/// Gets the field located at the specified position
		/// </summary>
		/// <param name="reader">
		/// A <see cref="BinaryReader"/> which will be used to read data. 
		/// </param>
		/// <param name="position">
		/// The position in the stream where the Field will be read from.
		/// </param>
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
				    Console.Error.WriteLine("ERR: Unsupported Field Type: " + fType);
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
				    Console.Error.WriteLine("ERR: Unsupported Field Type: " + fType);
                    Debugger.Break();
                    break;
            }
            return retval;
        }
		/// <summary>
		/// Gets the actual value for this field. Note that derived classes typically have type safe
		/// properties to access the data.
		/// </summary>
		public abstract object GetValue();
		
        public override string ToString()
        {
            return String.Format("{0}", GetValue());
        }
    }

	/// <summary>
	/// Represents a field in string format
	/// </summary>
    public class StringField : Field
    {
        public StringField(BinaryReader reader)
        {           
            ReadBasicProperties(reader);
            var strLength = reader.ReadInt16();
            var data = reader.ReadBytes(strLength);

            // For now, assume BOM mark is there and specifies LittleEndian UTF-16 (it is on my machine)
            Value = Encoding.Unicode.GetString(data, 2, data.Length - 2);
			if (Value != null)
				Value = Value.Trim();
        }

		/// <value>
		/// Gets the field data
		/// </value>
        public string Value { get; set; }

        public override object GetValue()
        {
            return Value;
        }
    }

	/// <summary>
	/// Represents a field which holds an integer
	/// </summary>
    public class IntegerField : Field
    {
        public IntegerField(BinaryReader reader)
        {
            ReadBasicProperties(reader);
            if (MaxSizeOnDisk != sizeof(int))
                Debugger.Break();
            Value = reader.ReadInt32();
        }
		/// <value>
		/// Gets the field data
		/// </value>
        public int Value { get; set; }

		public override object GetValue()
        {
            return Value;
        }
    }

	/// <summary>
	/// Represents a field which holds a date and time.
	/// </summary>
    public class DatetimeField : Field
    {
        private static DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        public DatetimeField(BinaryReader reader)
        {
            ReadBasicProperties(reader);
            int timestamp = reader.ReadInt32();
            Value = Epoch + TimeSpan.FromSeconds(timestamp);
        }
		/// <value>
		/// Gets the field data
		/// </value>
        public DateTime Value { get; set; }
		
        public override object GetValue()
        {
            return Value;
        }
    }
	
	/// <summary>
	/// Special field used by the first record. It holds the name 
	/// of the metadata fields which are available in all other
	/// records.
	/// </summary>
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
        /// <value>
        /// Gets the name of the metadata field
        /// </value>
        public string Value { get; private set; }
        
        public override object GetValue()
        {
            return Value;
        }
    }
	
	/// <summary>
	/// All the supported field types
	/// </summary>
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
