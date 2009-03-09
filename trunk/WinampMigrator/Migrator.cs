
using System;
using System.Collections.Generic;
using WinampReader;
using NDesk.Options;
namespace WinampMigrator
{	
	public class Migrator
	{
		public static string ProgramName { get { return "winamp-migrator"; } }
		private bool dryRun = true;		
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
		}
		
		public void Migrate()
		{
			using (BansheeDatabase banshee = new BansheeDatabase(bansheeDb))
			using (Table tbl = new Table(databaseFiles))
			{
				Logger.LogMessage(1, "Winamp DB successfully opened");
				foreach (Record row in tbl.Records)
				{
					StringField title  = row.GetFieldByType(MetadataField.Title) as StringField;
					StringField artist = row.GetFieldByType(MetadataField.Artist) as StringField;
					StringField album  = row.GetFieldByType(MetadataField.Album) as StringField;
					if (title == null || artist == null || album == null)
					{
						Logger.LogMessage(0, "Record does not contain title, artist & album");
						continue;
					}
					var track = banshee.GetTrack(title.Value, artist.Value, album.Value);
					if (track == null)
					{
						Logger.LogMessage(0, "Failed to find banshee track for {0} - {1} ({2})", artist, title, album);
						continue;
					}
					Logger.LogMessage(2, "Found Banshee Track ({0}) for {1} - {2} ({2})", track, artist, title, album);
				}
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
				{ "banshee-db=", "Specify the banshee db (default is $XDG_CONFIG_HOME/banshee-1/banshee.db)", v => bansheeDb = v },
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
			Console.WriteLine();
			Console.WriteLine("If no index is specified, it is assumed to reside in the same directory as the Winamp DB and have the extension .idx");
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
