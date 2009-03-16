
using System;
using System.Collections.Generic;
using WinampReader;
using NDesk.Options;
namespace WinampMigrator
{	
	public class Migrator
	{
		public static string ProgramName { get { return "winamp-migrator"; } }
		private bool dryRun		  = true;
		private bool dumpData 	  = false;
		private bool doBackup	  = true;
		private bool allowNoAlbum = false;
		private PlaycountUpdateMode playCountUpdateMode = PlaycountUpdateMode.Add;
		private RatingUpdateMode ratingUpdateMode = RatingUpdateMode.OnlyEmpty;
		private string bansheeDb;
		private WinampDatabase databaseFiles;
		
		
		public Migrator(string[] args)
		{
			ParseCommandLine(args);
			if (bansheeDb == null)
				bansheeDb = FindBansheeDb();
			if (bansheeDb == null)
			{
				Console.Error.WriteLine("Couldn't find Banshee db. Use --banshee-db to specify it");
				Environment.Exit((int)ExitCodes.InvalidBansheeDatabase);
			}
			// Try to open here to make sure the db exists and is valid
			try
			{
				var tbl = new Table(databaseFiles);
				tbl.Close();
			} 
			catch (ArgumentException ex)
			{
				Console.Error.WriteLine("Invalid winamp database specified: " + ex.Message);
				Environment.Exit((int)ExitCodes.InvalidWinampDatabase);
			}
			if (dumpData)
			{
				DumpWinampData();
				Environment.Exit((int)ExitCodes.NoError);
			}
		}
		
		public void DumpWinampData()
		{
			using (BansheeDatabase banshee = new BansheeDatabase(bansheeDb, (playCountUpdateMode == PlaycountUpdateMode.Overwrite)))
			using (Table tbl = new Table(databaseFiles))
			{
				foreach (Record row in tbl.Records)
				{					
					StringField title  = row.GetFieldByType(MetadataField.Title) as StringField;
					StringField artist = row.GetFieldByType(MetadataField.Artist) as StringField;
					StringField album  = row.GetFieldByType(MetadataField.Album) as StringField;
					StringField file   = row.GetFieldByType(MetadataField.Filename) as StringField;
					IntegerField rating = row.GetFieldByType(MetadataField.Rating) as IntegerField;
					IntegerField playcount = row.GetFieldByType(MetadataField.PlayCount) as IntegerField;
					Console.WriteLine("BEGIN TRACK");
					Console.WriteLine("File: {0}", file != null ? file.Value : "");
					Console.WriteLine("Title: {0}", title != null ? title.Value : "");
					Console.WriteLine("Artist: {0}", artist != null ? artist.Value : "");
					Console.WriteLine("Album: {0}", album != null ? album.Value : "");
					Console.WriteLine("Rating: {0}", rating != null ? rating.Value : -1);
					Console.WriteLine("Playcount: {0}", playcount != null ? playcount.Value : 0);
					
				}
			}
		}
		public void Migrate()
		{
			int succeeded = 0;
			int failed = 0;
			using (BansheeDatabase banshee = new BansheeDatabase(bansheeDb, (playCountUpdateMode == PlaycountUpdateMode.Overwrite)))
			using (Table tbl = new Table(databaseFiles))
			{
				Logger.LogMessage(1, "Winamp DB successfully opened");
				Logger.LogMessage(1, "Setting DryRun to {0}", dryRun);
				Logger.LogMessage(1, "Setting PlayCountUpdateMode to: {0}", playCountUpdateMode);
				Logger.LogMessage(1, "Setting RatingUpdateMode to: {0}", ratingUpdateMode);
				Logger.LogMessage(1, "Backing up Banshee DB: {0}", dryRun ? "Not Needed" : doBackup ? "Yes" : "No");
				Console.WriteLine("Press RETURN to migrate data or CTRL+C to abort now");
				Console.ReadLine();
				
				banshee.DryRun = dryRun;
				if (doBackup && !dryRun)
				{
					string copy = banshee.CreateBackup();
					Logger.LogMessage(1, "Backup of db created as {0}", copy);
				}
				foreach (Record row in tbl.Records)
				{
					StringField title  = row.GetFieldByType(MetadataField.Title) as StringField;
					StringField artist = row.GetFieldByType(MetadataField.Artist) as StringField;
					StringField album  = row.GetFieldByType(MetadataField.Album) as StringField;
					
					if (title == null || artist == null)
					{
						Logger.LogMessage(0, "Ignoring track since it lacks lacks title({0}) and/or artist({1})", title, artist);
						failed++;
						continue;
					}
							
					if (album == null)
					{
						if (allowNoAlbum)
							Logger.LogMessage(1, "{0} - {1} has no album info, but proceeding anyway", artist, title);
						else
						{
							Logger.LogMessage(0, "{0} - {1} lacks album info. Use --allow-no-album to migrate this track anyway", artist, title);
							failed++;
							continue;
						}
					}					
					BansheeTrack track;
					if (album != null)
						track = banshee.GetTrack(title.Value, artist.Value, album.Value);
					else
						track = banshee.GetTrack(title.Value, artist.Value);
					if (track == null)
					{
						Logger.LogMessage(0, "Failed to find banshee track for [{0}] - [{1}] [{2}]", artist, title, album);
						failed++;
						continue;
					}
					IntegerField ratingField = row.GetFieldByType(MetadataField.Rating) as IntegerField;
					IntegerField playcountField = row.GetFieldByType(MetadataField.PlayCount) as IntegerField;
					int rating = 0;
					int playcount = 0;
					if (ratingField != null)
						rating = ratingField.Value;
					if (playcountField != null)
						playcount = playcountField.Value;
					bool updateRating = false;
					bool updatePlaycount = false;
					
					if (playCountUpdateMode != PlaycountUpdateMode.Ignore)
						updatePlaycount = true;
					if (ratingUpdateMode == RatingUpdateMode.OnlyEmpty)
						updateRating = !track.HasRating;
					else if (ratingUpdateMode == RatingUpdateMode.Overwrite)
						updateRating = rating > 0;
					else if (ratingUpdateMode == RatingUpdateMode.OverwriteAndClear)
						updateRating = true;
					bool success = true;	
					if (updatePlaycount && updateRating)
						success = banshee.UpdateTrack(track.Id, rating, playcount);
					else if (updatePlaycount)
						success = banshee.UpdateTrackPlaycount(track.Id, playcount);
					else if (updateRating)
						success = banshee.UpdateTrackRating(track.Id, rating);
					if (success)
					{
						Logger.LogMessage(1, "SUCCEEDED: Updating {0}-{1}", artist.Value, title.Value);
						succeeded++;
					}
					else
					{
						Logger.LogMessage(0, "FAILED: Updating {0}-{1}", artist.Value, title.Value);					
					}
				}
				Logger.LogMessage(0, "All Done {0} tracks ({1:p}) successfully migrated ({2} ({3:p}) failed)", succeeded, succeeded / (double)tbl.NumFiles, failed, failed / (double)tbl.NumFiles);
			}
		}
				
		private string FindBansheeDb()
		{
			string confDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
			if (String.IsNullOrEmpty(confDir))
				confDir = System.IO.Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config");			
			
			foreach (string dir in System.IO.Directory.GetDirectories(confDir))
			{
				string dirName = System.IO.Path.GetFileName(dir);
				if (dirName.StartsWith("banshee", StringComparison.InvariantCultureIgnoreCase))
					return System.IO.Path.Combine(dir, "banshee.db");
			}
			return null;
		}
		/// <summary>
		/// Parses the command line options. This method will exit the program if it encounters any errors or invalid args.
		/// </summary>
		/// <param name="args">
		/// The arguments that were passed to Main()
		/// </param>
		private void ParseCommandLine(string[] args)
		{
			bool show_help = false;
			var p = new OptionSet()
			{
				{ "dry-run", "Don't write to Banshee DB, only simulate", v => dryRun = true },
				{ "h|help", "Show this help and then exit", v => show_help = (v != null) },
				{ "dump-data", "Dump WinAmp data to stdout and exit", v => dumpData = true },
				{ "allow-no-album", "Use title+artist matching if album isn't set", v => allowNoAlbum = true },
				{ "backup-db", "Create a backup copy of the Banshee db before modifying it", v => doBackup = true },
				{ "banshee-db=", "Specify the banshee db (default is $XDG_CONFIG_HOME/banshee-1/banshee.db)", v => bansheeDb = v },
				{ "update-playcount=", "Specifies how & if banshee's playcount is updated ('ignore', 'overwrite', 'add' (default))", 
					v => 
					{
						if (v == null)
							return;			
						string mode = v.ToString().ToLower();
						if (mode == "ignore")
							playCountUpdateMode = PlaycountUpdateMode.Ignore;
						else if (mode == "overwrite")
							playCountUpdateMode = PlaycountUpdateMode.Overwrite;
						else
							playCountUpdateMode = PlaycountUpdateMode.Add;
					}
				},
				{ "update-rating=", "Specifies how & if banshee's rating is update ('ignore', 'overwrite' or 'empty' (default))", 
					v => 
					{
						if (v == null)
							return;
						string mode = v.ToString().ToLower();
						if (mode == "ignore")
							ratingUpdateMode = RatingUpdateMode.Ignore;
						else if (mode == "overwrite")
							ratingUpdateMode = RatingUpdateMode.Overwrite;
						else if (mode == "overwriteandclear")
							ratingUpdateMode = RatingUpdateMode.OverwriteAndClear;
						else
							ratingUpdateMode = RatingUpdateMode.OnlyEmpty;
					}
				},
				{ "V|verbose", "Increase verbosity (specify multiple times to increase further)", v => { if (v != null) Logger.Verbosity++; } },
			};
			
			List<string> extra = null;
			try
			{				
				extra = p.Parse(args);
			}
			catch (OptionException ex)
			{
				ShowOptionError(ex.Message);
				Environment.Exit((int)ExitCodes.InvalidArguments);
			}			
			if (show_help)
			{
				ShowUsage(p);
				Environment.Exit((int)ExitCodes.NoError);
			}
			if (extra.Count == 0)
			{
				ShowOptionError("No winamp database specified");
				Environment.Exit((int)ExitCodes.InvalidArguments);
			}
			if (extra.Count == 1)
				databaseFiles = new WinampDatabase(extra[0]);
			else if (extra.Count == 2)
				databaseFiles = new WinampDatabase(extra[0], extra[1]);
			else
			{
				ShowOptionError("Unknown arguments: " + String.Join(",", extra.ToArray()));
				Environment.Exit((int)ExitCodes.InvalidArguments);
			}
			if (!databaseFiles.Exists)
			{
				Console.Error.WriteLine("The specified winamp database does not exist");
				Environment.Exit((int)ExitCodes.InvalidArguments);
			}

		}
		
		private void ShowOptionError(string errMessage)
		{
			Console.Error.Write("{0}: ", ProgramName);
			Console.Error.WriteLine(errMessage);
			Console.Error.WriteLine("Try `{0} --help' for more help", ProgramName);
		}
		
		private void ShowUsage(OptionSet opts)
		{
			Console.WriteLine("Usage: {0} [OPTIONS] <WinampDb> [<WinampDbIndex>]", ProgramName);
			Console.WriteLine(" Where OPTIONS is one or more of the following:");
			opts.WriteOptionDescriptions(Console.Out);
			
			Console.WriteLine("Ratings can be updated in four modes:");
			Console.WriteLine("  Ignore:            no updates to rating will be done");
			Console.WriteLine("  Overwrite:         All ratings found in Winamp will be forced into Banshee");
			Console.WriteLine("  OverwriteAndClear: Same as 'Overwrite' but will also clear ratings");
			Console.WriteLine("                     in Banshee where no rating exists in Winamp");
			Console.WriteLine("  OnlyEmpty:         Only tracks which has no rating in banshee will");
			Console.WriteLine("                     be updated. (default)");
			
			Console.WriteLine("PlayCount can be updated in three modes:");
			Console.WriteLine("  Ignore:            No updates to playcount will be done");
			Console.WriteLine("  Overwrite:         Winamp playcount will be forced into Banshee");
			Console.WriteLine("  Add:               Winamp playcount will be added to the Banshee");
			Console.WriteLine("                     playcount (default)");
			Console.WriteLine();
			
			Console.WriteLine("If no WinampDbIndex is specified, it is assumed to reside in the same directory as the WinampDB and have the extension .idx");
		}
		
		static void Main(string[] args)
		{
			try
			{	
				var migrator = new Migrator(args);
				migrator.Migrate();
			}
			catch (ApplicationException ex)
			{
				Console.Error.WriteLine("Error migrating data: " + ex.Message);
				Environment.Exit((int) ExitCodes.Failed);
			}			
		}
		
		private enum ExitCodes : int
		{
			NoError = 0,
			Failed,
			InvalidArguments,
			InvalidWinampDatabase,
			InvalidBansheeDatabase,
		}
				
		private enum PlaycountUpdateMode
		{
			Add,
			Overwrite,
			Ignore,
		}
	
		private enum RatingUpdateMode
		{
			OnlyEmpty,
			Overwrite,
			OverwriteAndClear,
			Ignore,			
		}
	}
	
	public sealed class Logger
	{
		public static int Verbosity { get; set; }

		public static void LogMessage(int atLevel, string format, params object[] args)
		{
			LogMessage(atLevel, String.Format(format, args));
		}
		
		public static void LogMessage(int atLevel, string message)
		{
			if (Verbosity < atLevel)
				return;
			Console.WriteLine(message);
		}
		
	}
}
