// Track.cs
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

namespace WinampReader
{
	/// <summary>
	/// Class represents a track, with all metadata
	/// </summary>	
	public class Track
	{	
		/// <summary>
		/// Creates an empty track instance
		/// </summary>
		public Track()
		{
		}
		
		/// <summary>
		/// Creates a new Track instance filled with the values found in the specified record.
		/// </summary>
		/// <param name="row">The record containing the track information</param>
		public Track(Record row)
		{
			Title = GetStringValue(row, MetadataField.Title);
			Album = GetStringValue(row, MetadataField.Album);
			Artist = GetStringValue(row, MetadataField.Artist);
			AlbumArtist = GetStringValue(row, MetadataField.AlbumArtist);
			PlayCount = GetIntValue(row, MetadataField.PlayCount, 0);
			Rating = GetIntValue(row, MetadataField.Rating);
		}
		
		private string GetStringValue(Record row, MetadataField fieldType)
		{
			StringField field = (StringField) row.GetFieldByType(fieldType);
			if (field != null)
				return field.Value;
			return null;
		}
		
		private int? GetIntValue(Record row, MetadataField fieldType)
		{
			IntegerField field = (IntegerField) row.GetFieldByType(fieldType);
			if (field != null)
				return field.Value;
			return null;
		}
		
		private int GetIntValue(Record row, MetadataField fieldType, int defaultValue)
		{
			int? val = GetIntValue(row, fieldType);
			if (val.HasValue)
				return val.Value;
			
			return defaultValue;				
		}
		
		/// <value>
		/// The title of the track
		/// </value>
		public string Title { get; set; }
		/// <summary>
		/// The artist performing the track
		/// </summary>
		public string Artist { get; set; }
		/// <value>
		/// The album the track is part on
		/// </value>
		public string Album { get; set; }
		/// <value>
		/// The album artist (in case of e.g. compilation albums) for the album where the track originated from.
		/// </value>
		public string AlbumArtist { get; set; }
		/// <value>
		/// The rating of the track
		/// </value>
		public int? Rating { get; set; }
		/// <summary>
		/// The number of times the track has been played
		/// </summary>
		public int PlayCount { get; set; }
		
		
		public override string ToString ()
		{
			return string.Format("[Track: Title={0}, Artist={1}, Album={2}, AlbumArtist={3}, Rating={4}, PlayCount={5}]", Title, Artist, Album, AlbumArtist, Rating, PlayCount);
		}
	}
}
