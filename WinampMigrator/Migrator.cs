
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
		private int verbosity;
		private string bansheeDb;
		private WinampDatabase databaseFiles;
		
		public Migrator(string[] args)
		{
			ParseCommandLine(args);			
			Table tbl = null;
			try
			{
				tbl = new Table(databaseFiles);
			} catch (ArgumentException ex)
			{
				Console.Error.WriteLine("Invalid winamp database specified: " + ex.Message);
				Environment.Exit((int)ExitCodes.InvalidWinampDatabase);
			}			
			LogMessage(1, "Number of files in winamp db: {0}", tbl.NumFiles);
			
			tbl.Close();
		}
		
		private void LogMessage(int atLevel, string format, params object[] args)
		{
			LogMessage(atLevel, String.Format(format, args));
		}
		
		private void LogMessage(int atLevel, string message)
		{
			if (verbosity < atLevel)
				return;
			Console.WriteLine(message);
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
				{ "V|verbose", "Increase verbosity (specify multiple times to increase further)", v => { if (v != null) verbosity++; } },
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
		}
		
		static void Main(string[] args)
		{
			try
			{	
				new Migrator(args);
			}
			catch (Exception ex)
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
		}
	}
}
