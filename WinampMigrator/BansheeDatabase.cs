
using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;

namespace WinampMigrator
{		
	public class BansheeDatabase : IDisposable
	{	
		IDbConnection dbConn;
		List<IDbCommand> commands = new List<IDbCommand>();
		IDbCommand selectCmd, selectAlbumCmd, selectArtistCmd;
		IDbCommand updateAllCmd, updateRatingCmd, updatePlaycountCmd;
		
		public BansheeDatabase(string dbFile)
			: this(dbFile, false)
		{
			
		}
		
		public BansheeDatabase(string dbFile, bool overwritePlaycount)
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
			CreateCommands(overwritePlaycount);
		}
		
		private void CreateCommands(bool overwritePlaycount)
		{
			if (selectCmd != null)
				throw new InvalidOperationException("Commands are already created");
			selectCmd = dbConn.CreateCommand();
			selectCmd.CommandText = "SELECT TrackID, PlayCount, Rating FROM CoreTracks WHERE Title = @title AND ArtistID = @artist AND AlbumID = @album";
			selectCmd.Parameters.Add(new SqliteParameter("@title", DbType.String));
			selectCmd.Parameters.Add(new SqliteParameter("@artist", DbType.Int32));
			selectCmd.Parameters.Add(new SqliteParameter("@album", DbType.Int32));			
			commands.Add(selectCmd);
			
			selectArtistCmd = dbConn.CreateCommand();
			selectArtistCmd.CommandText = "SELECT ArtistID From CoreArtists WHERE Name = @name";
			selectArtistCmd.Parameters.Add(new SqliteParameter("@name", DbType.String));
			commands.Add(selectArtistCmd);
			
			selectAlbumCmd = dbConn.CreateCommand();
			selectAlbumCmd.CommandText = "SELECT AlbumID From CoreAlbums WHERE ArtistID = @artist AND Title = @title";
			selectAlbumCmd.Parameters.Add(new SqliteParameter("@artist", DbType.Int32));
			selectAlbumCmd.Parameters.Add(new SqliteParameter("@title", DbType.String));
			commands.Add(selectAlbumCmd);
			
			updateAllCmd = dbConn.CreateCommand();
			if (overwritePlaycount)
				updateAllCmd.CommandText = "UPDATE CoreTracks SET Rating=@rating, PlayCount=@playcount WHERE TrackID=@trackid";
			else
				updateAllCmd.CommandText = "UPDATE CoreTracks SET Rating=@rating, PlayCount=PlayCount + @playcount WHERE TrackID=@trackid";
			updateAllCmd.Parameters.Add(new SqliteParameter("@rating", DbType.Int32));
			updateAllCmd.Parameters.Add(new SqliteParameter("@playcount", DbType.Int32));
			updateAllCmd.Parameters.Add(new SqliteParameter("@trackid", DbType.Int32));
			commands.Add(updateAllCmd);
			
			updatePlaycountCmd = dbConn.CreateCommand();
			if (overwritePlaycount)
				updatePlaycountCmd.CommandText = "UPDATE CoreTracks SET PlayCount=@playcount WHERE TrackID=@trackid";
			else
				updatePlaycountCmd.CommandText = "UPDATE CoreTracks SET PlayCount=PlayCount + @playcount WHERE TrackID=@trackid";
			updatePlaycountCmd.Parameters.Add(new SqliteParameter("@playcount", DbType.Int32));
			updatePlaycountCmd.Parameters.Add(new SqliteParameter("@trackid", DbType.Int32));
			commands.Add(updatePlaycountCmd);
			
			updateRatingCmd = dbConn.CreateCommand();
			updateRatingCmd.CommandText = "UPDATE CoreTracks SET Rating=@rating WHERE TrackID=@trackid";
			updateRatingCmd.Parameters.Add(new SqliteParameter("@rating", DbType.Int32));
			updateRatingCmd.Parameters.Add(new SqliteParameter("@trackid", DbType.Int32));
			commands.Add(updateRatingCmd);			
		}
		
		/// <value>
		/// Gets or sets a value indicating whether updates to the db should be applied (false) or if just simulated (true)
		/// </value>
		public bool DryRun { get; set; }
		
		public BansheeTrack GetTrack(string title, string artist, string album)
		{
			int artistId = GetArtistId(artist);
			int albumId = GetAlbumId(album, artistId);
			if (artistId < 0 || albumId < 0)
				return null;
			
			Logger.LogMessage(2,"Fetching Track Info for {0}, {1}, {2}", title, artistId, albumId);
			
			((IDataParameter)selectCmd.Parameters["@title"]).Value = title;
			((IDataParameter)selectCmd.Parameters["@artist"]).Value = artistId;
			((IDataParameter)selectCmd.Parameters["@album"]).Value = albumId;
			using (var reader = selectCmd.ExecuteReader())
			{
				if (reader.Read())
				{
					var trackId = reader.GetInt32(0);
					var playCount = reader.GetInt32(1);
					var rating = reader.GetInt32(2);					
					return new BansheeTrack(trackId, rating > 0, playCount > 0);
				}
			}
			return null;
		}
		
		/// <summary>
		/// Updates the track information with the specified rating and playcount
		/// </summary>
		/// <param name="trackID">The ID of the track to update</param>
		/// <param name="rating"></param>
		/// <param name="playcount"></param>
		/// <returns>
		/// True if the track info was successfully updated
		/// </returns>
		public bool UpdateTrack(int trackID, int rating, int playcount)
		{
			Logger.LogMessage(2,"Updating track {0} with rating={1}, playcount={2}", trackID, rating, playcount);
			
			((IDataParameter)updateAllCmd.Parameters["@rating"]).Value = rating;
			((IDataParameter)updateAllCmd.Parameters["@playcount"]).Value = playcount;
			((IDataParameter)updateAllCmd.Parameters["@trackid"]).Value = trackID;
			return RunUpdateCommand(updateAllCmd);
		}
		
		/// <summary>
		/// Updates the track's rating
		/// </summary>
		public bool UpdateTrackRating(int trackID, int rating)
		{
			Logger.LogMessage(2,"Updating track {0} with rating={1}", trackID, rating);
			
			((IDataParameter)updateRatingCmd.Parameters["@rating"]).Value = rating;
			((IDataParameter)updateRatingCmd.Parameters["@trackid"]).Value = trackID;
			return RunUpdateCommand(updateRatingCmd);
		}
		
		/// <summary>
		/// Updates the track's playcount
		/// </summary>
		public bool UpdateTrackPlaycount(int trackID, int playcount)
		{
			Logger.LogMessage(2,"Updating track {0} with playcount={1}", trackID, playcount);
			
			((IDataParameter)updatePlaycountCmd.Parameters["@playcount"]).Value = playcount;
			((IDataParameter)updatePlaycountCmd.Parameters["@trackid"]).Value = trackID;
			return RunUpdateCommand(updatePlaycountCmd);	
		}
		
		private bool RunUpdateCommand(IDbCommand cmd)
		{
			if (DryRun)
				return true;
			
			var rowsAffected = cmd.ExecuteNonQuery();
			if (rowsAffected == 1)
				return true;
			else if (rowsAffected > 1) {
				object trackid = ((IDataParameter)cmd.Parameters["@rating"]).Value;
				Logger.LogMessage(0, "ERR: Trying to update track {0} with rating & playcount affected {1} rows", trackid, rowsAffected);
			}
			return false;
		}
		
		private int GetArtistId(string name)
		{
			((IDataParameter)selectArtistCmd.Parameters["@name"]).Value = name;
			Logger.LogMessage(3, "Fetching artist id for {0}", name);
			object val = selectArtistCmd.ExecuteScalar();
			if (val == null)
			{
				Logger.LogMessage(0, "WRN: Couldn't find artist id for {0}", name);
				return -1;
			}
			int artistId;
			if (Int32.TryParse(val.ToString(), out artistId))
				return artistId;
			
			Logger.LogMessage(0, "Got {0} ({1}) in return which cannot be converted to Int32", val, val.GetType());
			return -1;
		}
		
		private int GetAlbumId(string albumTitle, int artistId)
		{
			Logger.LogMessage(3, "Fetching album id for {0} (artist = {1})", albumTitle, artistId);

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
		
		#region	IDisposable Members
		bool disposed;
		void IDisposable.Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		private void Dispose(bool disposing)
		{
			if (disposed)
				return;
			if (disposing)
			{
				if (dbConn != null)
					dbConn.Close();
				dbConn = null;
				
				foreach (var cmd in commands)
					cmd.Dispose();
				
				disposed = true;
			}
		}
		#endregion
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
