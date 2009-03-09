
using System;
using System.Data;
using Mono.Data.Sqlite;

namespace WinampMigrator
{		
	public class BansheeDatabase : IDisposable
	{	
		IDbConnection dbConn;
		IDbCommand selectCmd;
		
		public BansheeDatabase(string dbFile)
		{
			Logger.LogMessage(1, "Loading Banshee DB: {0}", dbFile);
			try
			{
				dbConn = new SqliteConnection (String.Format ("Version=3,URI=file:{0}", dbFile));				
				dbConn.Open();
			} 
			catch (Exception ex)
			{
				Logger.LogMessage(0, "Failed to open banshee db: {0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message);
				throw new ApplicationException("Failed to open banshee db", ex);
			}
		}
		
		public BansheeTrack GetTrack(string title, string artist, string album)
		{
			int artistId = GetArtistId(artist);
			int albumId = GetAlbumId(album, artistId);
			if (selectCmd == null)
			{
				selectCmd = dbConn.CreateCommand();
				selectCmd.CommandText = "SELECT TrackID, PlayCount, Rating FROM CoreTracks WHERE Title = @title AND ArtistID = @artist AND AlbumID = @album";
				selectCmd.Parameters.Add(new SqliteParameter("@title", DbType.String));
				selectCmd.Parameters.Add(new SqliteParameter("@artist", DbType.Int32));
				selectCmd.Parameters.Add(new SqliteParameter("@album", DbType.Int32));
			}
			selectCmd.Connection = dbConn;
			Logger.LogMessage(2,"Fetching Track Info for {0}, {1}, {2}", title, artistId, albumId);
			((SqliteParameter)selectCmd.Parameters["@title"]).Value = title;
			((SqliteParameter)selectCmd.Parameters["@artist"]).Value = artistId;
			((SqliteParameter)selectCmd.Parameters["@album"]).Value = albumId;
			using (var reader = selectCmd.ExecuteReader())
			{
				Logger.LogMessage(2, "Select tracks returned a reader of depth {0}", reader.Depth);
				if (reader.Depth == 0)
					return null;
				return new BansheeTrack(reader.GetInt32(0), reader.GetInt32(1) > 0, reader.GetInt32(2) > 0);
			}
		}
		
		private IDbCommand selectArtistCmd;
		private int GetArtistId(string name)
		{
			if (selectArtistCmd == null)
			{
				selectArtistCmd = dbConn.CreateCommand();
				selectArtistCmd.CommandText = "SELECT ArtistID From CoreArtists WHERE Name = @name";
				selectArtistCmd.Parameters.Add(new SqliteParameter("@name", DbType.String));
			}
			selectArtistCmd.Connection = dbConn;
			((SqliteParameter)selectArtistCmd.Parameters["@name"]).Value = name;
			Logger.LogMessage(3, "Fetching artist id for {0}", name);
			object val = selectArtistCmd.ExecuteScalar();
			int artistId;
			if (Int32.TryParse(val.ToString(), out artistId))
				return artistId;
			
			Logger.LogMessage(0, "Got {0} ({1}) in return which cannot be converted to Int32", val, val.GetType());
			return -1;
		}
		
		private IDbCommand selectAlbumCmd;
		private int GetAlbumId(string albumTitle, int artistId)
		{
			Logger.LogMessage(3, "Fetching album id for {0} (artist = {1})", albumTitle, artistId);
			if (selectAlbumCmd == null)
			{
				selectAlbumCmd = dbConn.CreateCommand();
				selectAlbumCmd.CommandText = "SELECT AlbumID From CoreAlbums WHERE ArtistID = @artist AND Title = @title";
				selectAlbumCmd.Parameters.Add(new SqliteParameter("@artist", DbType.Int32));
				selectAlbumCmd.Parameters.Add(new SqliteParameter("@title", DbType.String));
			}
			((SqliteParameter)selectAlbumCmd.Parameters["@artist"]).Value = artistId;
			((SqliteParameter)selectAlbumCmd.Parameters["@title"]).Value = albumTitle;
			selectAlbumCmd.Connection = dbConn;
			
			object val = selectAlbumCmd.ExecuteScalar();
			if (val == null)
				return -1;
			int albumId;
			if (Int32.TryParse(val.ToString(), out albumId))
				return albumId;

			Logger.LogMessage(0, "Got {0} ({1}) in return which cannot be converted to Int32", val, val.GetType());

			return -1;
		}

		public void Close()
		{
			Dispose(true);
		}
		
		
		void IDisposable.Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (dbConn != null)
					dbConn.Close();
				dbConn = null;
				
				if (selectAlbumCmd != null)
					selectAlbumCmd.Dispose();
				if (selectArtistCmd != null)
					selectArtistCmd.Dispose();
				if (selectCmd != null)
					selectCmd.Dispose();
				
				selectAlbumCmd = selectArtistCmd = selectCmd = null;
			}
		}
	}
	
	public class BansheeTrack
	{
		public BansheeTrack(int id, bool hasRating, bool hasPlayCount)
		{
			Id = id;
			HasRating = hasRating;
			HasPlayCount = hasPlayCount;
		}
		public int Id { get; private set; }
		public bool HasRating { get; private set; }
		public bool HasPlayCount { get; private set; }
		public override string ToString ()
		{
			return string.Format("[BansheeTrack: Id={0}, HasRating={1}, HasPlayCount={2}]", Id, HasRating, HasPlayCount);
		}

	}
}
