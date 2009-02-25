// RecordEnumerator.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinampReader
{
    public class RecordEnumerator : IEnumerable<Record>
    {
        public Table ParentTable { get; set; }
        public RecordEnumerator(Table parentTable)
        {
            this.ParentTable = parentTable;
        }
        
        #region IEnumerable<Record> Members
        public IEnumerator<Record> GetEnumerator()
        {
            for (int idx = 2; idx < ParentTable.Index.NumEntries; idx++)
            {
                var position = ParentTable.Index.GetIndex(idx);
                yield return new Record(ParentTable.Reader, position, ParentTable.FieldMappings);
            }
        }
        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
