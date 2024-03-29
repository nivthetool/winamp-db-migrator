// Record.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WinampReader
{
	/// <summary>
	/// Represents a single record (row) in the database table.
	/// </summary>
    public class Record
    {
        private List<Field> fields;
        private MetadataFieldMapping fieldMapping;

        public Record(BinaryReader reader, int position, MetadataFieldMapping fieldMapping)
        {
            this.fieldMapping = fieldMapping;
            fields = new List<Field>();
            int curPos = position;
            while (curPos > 0)
            {
                var f = Field.GetField(reader, curPos);
                curPos = f.NextFieldPos;
                fields.Add(f);
            }
        }

		/// <summary>
		/// Gets the value for the specified metadata field
		/// </summary>
		/// <param name="field">
		/// A <see cref="MetadataField"/> specifying which metadata value to retrieve.
		/// </param>
		/// <returns>
		/// A <see cref="Field"/> containing the actual data.
		/// </returns>
        public Field GetFieldByType(MetadataField field)
        {
            if (fieldMapping.ContainsKey(field))
                return GetFieldById(fieldMapping[field]);
            return null;
        }

		/// <summary>
		/// Gets the value from the specified field id
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.UInt64"/> 
		/// </param>
		/// <returns>
		/// A <see cref="Field"/>
		/// </returns>
        public Field GetFieldById(ulong id)
        {
            foreach (var field in Fields)
                if (field.Id == id)
                    return field;
            return null;
        }
		/// <value>
		/// Gets an enumerator over all the available fields in this record
		/// </value>
        public IEnumerable<Field> Fields
        {
            get { return fields; }
        }
    }
}
