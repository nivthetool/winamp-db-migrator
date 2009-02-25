// Index.cs
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
using System.IO;
using System.Collections.Generic;

namespace WinampReader
{	
	public class Index
	{
		private static string INDEX_SIGNATURE = "NDEINDEX";
		//private static Int32 BLOCK_SIZE = 2048;
		private Int32[] _indexTable;
		public Int32 NumEntries {get; private set; }
		public string Filename { get; private set; }
		public int Id { get; private set; }
		public Index(string filename)
		{
			Filename = filename;
			Load();
		}
		
		private void Load()
		{
			using (BinaryReader reader = new BinaryReader(File.OpenRead(Filename)))
			{
				if (!ReadSignature(reader.BaseStream))
					throw new ArgumentException("File is not a valid WinAmp Index file", "filename");
				NumEntries = reader.ReadInt32();
				Id = reader.ReadInt32();
				// Ok, now we're at the position of the index data, suck it all in!
				_indexTable = new int[NumEntries*2];
				for (int pos = 0; pos < _indexTable.Length; pos++)
				{
					_indexTable[pos] = reader.ReadInt32();
				}
			}
		}
		
		private bool ReadSignature(Stream stream)
		{
			
			byte [] sig = new byte[INDEX_SIGNATURE.Length];
			stream.Read(sig, 0, sig.Length);
			if (System.Text.ASCIIEncoding.ASCII.GetString(sig) != INDEX_SIGNATURE)
				return false;					
			return true;
		}
		
		public int GetIndex(int Idx)
		{
			if (Idx < 0 || (Idx*2) > _indexTable.Length)
				throw new ArgumentOutOfRangeException("Idx");
			return _indexTable[Idx*2];
		}
		
		public void Dump()
		{
			for (int i = 0; i < NumEntries; i++)
				Console.WriteLine("Index: {0:000} = {1:000}", i, GetIndex(i));
		}
	}
}
