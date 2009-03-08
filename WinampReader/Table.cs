// Table.cs
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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace WinampReader
{
	/// <summary>
	/// This is the main class of this library. It handles reading a table from disk
	/// and holds the index.
	/// </summary>
	public class Table : IDisposable
	{
		/// <summary>
		/// Create a new Table instance from the database and loads its corresponding index file
		/// </summary>
		/// <param name="filename">
		/// A <see cref="System.String"/> with the full path to the winamp (.dat) database. A corresponding .idx file
		/// should also be located in that same directory. 
		/// </param>
		public Table(string filename)
		{
            Filename = filename;
			Reader = new BinaryReader(File.OpenRead(filename));
            byte[] data = Reader.ReadBytes("NDETABLE".Length);
            if (Encoding.ASCII.GetString(data) != "NDETABLE")
                throw new ArgumentException("File is not a valid WinAmp media library database");
            string indexName = Path.ChangeExtension(filename, ".idx");
            Index = new Index(indexName);

            var fieldPointerRecord = new Record(Reader, Index.GetIndex(0), null);
            FieldMappings = new MetadataFieldMapping();
            foreach (ColumnField col in fieldPointerRecord.Fields)
                FieldMappings.Add(col.Value, col.Id);
		}

		/// <value>
		/// Gets the index for this db
		/// </value>
        public Index Index { get; private set; }
		/// <value>
		/// Gets the reader which is used to read data from the db.
		/// </value>
        public BinaryReader Reader { get; private set; }
		/// <value>
		/// Gets the filename from which this table was loaded.
		/// </value>
        public string Filename { get; private set; }
		/// <value>
		/// Gets the number of files in the media library
		/// </value>
        public int NumFiles { get { return Index.NumEntries - 2; } }

        /// <summary>
        /// Gets the mapping between a particular field and it's Id (position) within a record
        /// </summary>
        public MetadataFieldMapping FieldMappings { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reader.Close();
                Reader = null;
            }
        }
        #endregion
    }

	/// <summary>
	/// Holds a mapping between metadata fields ("artist", "album", "title", etc.) and
	/// a pointer to where they are located within each record ("row") in the table.
	/// </summary>
    public class MetadataFieldMapping : Dictionary<MetadataField, uint>
    {
        /// <summary>
        /// Adds the specified key to the dictionary. The string key is automatically converted to 
        /// a MetadataField enum
        /// </summary>
        /// <param name="key">String representation of key name</param>
        /// <param name="value">The ID of the field</param>
        public void Add(string key, uint value)
        {
            try
            {
                var field = (MetadataField)Enum.Parse(typeof(MetadataField), key, true);
                this.Add(field, value);
            }
            catch { }
        }
    }
    /// <summary>
    /// Represents a particular metadata field in a record
    /// </summary>
    public enum MetadataField
    {
        Album,
        AlbumArtist,
        Artist,
        Bitrate,
        Bpm,
        Comment,
        Composer,
        Filename,
        Filesize,
        Filetipe,
        Genre,
        LastPlay,
        Length,
        PlayCount,
        Publisher,
        Rating,
        Title,
        TrackNo,
        Tracks,
        Type,
        Year,
    }
	
	public class WinampDatabase
	{		
		/// <summary>
		/// Creates a new instance if the WinampDatabase class 
		/// </summary>
		/// <param name="dbFileName">Full path to the database file. Index file is assumed to be in the same directory, but with the .idx extension</param>
		public WinampDatabase(string dbFileName)
		{
			Database = dbFileName;
			Index = Path.ChangeExtension(dbFileName, ".idx");
		}
		
		/// <summary>
		/// Creates a new instance if the WinampDatabase class 
		/// </summary>
		/// <param name="dbFileName">Full path to the database file</param>
		/// <param name="dbIndexName">Full path to the index file</param>
		public WinampDatabase(string dbFileName, string dbIndexName)
		{
			Database = dbFileName;
			Index = dbIndexName;
		}
		
		///<summary>
		/// The filename of the actual database file 
		///</summary>
		public string Database { get; set; }
		///<summary>
		/// The filename of the index file for the database 
		///</summary>
		public string Index { get; set; }
	}
}
