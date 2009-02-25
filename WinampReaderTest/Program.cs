// Program.cs
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

namespace WinampReaderTest
{
    using WinampReader;
    class Program
    {
        static void Main(string[] args)
        {
            int count = 0;
            if (args.Length == 0)
			{				
				Console.WriteLine("Usage: winamptest <DATFILE>", args.Length);
				return;		
			}
            using (var table = new Table(args[0]))
            {
                Console.WriteLine("Table {0} contains {1} entries", table.Filename, table.NumFiles);
                var items = new RecordEnumerator(table);
                foreach (Record record in items)
                {
                    count++;             
                    Console.WriteLine("====== [ Entry ] ======");
                    Console.WriteLine("Artist: {0}", record.GetFieldByType(MetadataField.Artist));
                    Console.WriteLine("Album:  {0}", record.GetFieldByType(MetadataField.Album));
                    Console.WriteLine("Title:  {0}", record.GetFieldByType(MetadataField.Title));
                    Console.WriteLine("Rating: {0}", record.GetFieldByType(MetadataField.Rating));
                    Console.WriteLine();                    
                }
                Console.WriteLine("Reading done, {0} out of {1} items scanned", count, table.NumFiles);
            }
            
            Console.ReadLine();
        }
    }
}
