using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using Web.Utilities;

namespace Web
{
	public class CommandProcessor
	{
		private const string Tab = "&nbsp;&nbsp;&nbsp;&nbsp;";
		private static readonly List<string> BannedCommands = new List<string> { "chmod", "shutdown", "del", "rm" };
		private static readonly TwoWayMap<string, CommandType> CommandMap = PopulateCommandMap();
		private static readonly Lazy<List<string>> CommandsDescriptions = new Lazy<List<string>>(() =>
		{
			var values = ((CommandType[])Enum.GetValues(typeof(CommandType)))
				// invalid is not actually a command
				.Where(v => v != CommandType.Invalid)
				.ToList();

			var descriptions = values.Select(GetCommandDescription).ToList();
			// this is not a built in command, since it doesn't come to the server (and we insert here so that it is properly ordered)
			descriptions.Insert(descriptions.Count - 2, "clear - clears the output content of the terminal");
			return descriptions;
		});

		private readonly DirectoryManager _directoryManager;
		private readonly UrlHelper _urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);

		public CommandProcessor(DirectoryManager directoryManager)
		{
			this._directoryManager = directoryManager;
		}

		public Command Parse(string input)
		{
			if (input.IsNullOrWhiteSpace())
			{
				return new Command { Type = CommandType.Invalid };
			}

			// special case for ./{file} since the command touches the argument by manually adding a space so the split operates as normal
			if (input.StartsWith("./"))
			{
				input = input.Insert(2, " ");
			}

			var words = input.Trim().Split();
			var command = words.First().ToLowerInvariant();
			var arguments = words.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

			// convert to typed command
			CommandType commandType;
			if (!CommandMap.Forward.TryGetValue(command, out commandType))
			{
				commandType = CommandType.Invalid;
			}

			return new Command
			{
				Type = commandType,
				CommandString = command,
				Arguments = arguments
			};
		}

		public CommandResult Execute(Command command, string currentPath)
		{
			switch (command.Type)
			{
				case CommandType.Invalid:
					return new CommandResult
					{
						ResponseLines = new List<string> { $"Invalid command: '{command.CommandString}'. Type 'help' for list of commands." },
						Color = Color.Red
					};
				case CommandType.GetDirectoryContents:
					return this.ExecuteGetDirectoriesAndFiles(currentPath);
				case CommandType.PrintCurrentDirectory:
					var currentDirectory = this._directoryManager.GetDirectoryInfo(currentPath).Name;
					return new CommandResult
					{
						ResponseLines = new List<string> { currentDirectory }
					};
				case CommandType.ReadFile:
					return this.ExecuteReadFile(command, currentPath);
				case CommandType.ChangeDirectories:
					return this.ExecuteChangeDirectory(command, currentPath);
				case CommandType.RunShellScript:
					return ExecuteRunShellScript(command, currentPath);
				case CommandType.RunExecutable:
					return ExecuteRunExecutable(command, currentPath);
				case CommandType.Help:
					return new CommandResult
					{
						ResponseLines = CommandsDescriptions.Value
					};
				case CommandType.Exit:
					var url = this._urlHelper.Action("Home", "Home", routeValues: null, protocol: HttpContext.Current.Request.Url.Scheme);
					return new CommandResult
					{
						ResponseLines = new List<string> { "Exiting terminal and redirecting to web view..." },
						RedirectUrl = new Uri(url ?? "http://ryanallen.io")
					};
				default:
					throw new InvalidOperationException($"Invalid {typeof(CommandType).FullName}: '{command.Type}'");
			}
		}

		public string TabComplete(string path, string word)
		{
			DirectoryInfo directory;
			if (string.IsNullOrWhiteSpace(word) || !this._directoryManager.TryGetDirectoryInfo(path, out directory))
			{
				return null;
			}
			var subDirectories = directory.Directories;
			subDirectories.AddRange(directory.Files.Select(f => f.Name));
			var result = subDirectories
				.Where(d => d.StartsWith(word, comparisonType: StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(d => d)
				.FirstOrDefault();

			return result;
		}

		private CommandResult ExecuteGetDirectoriesAndFiles(string currentPath)
		{
			var responseLines = new List<string>();
			var directory = this._directoryManager.GetDirectoryInfo(currentPath);
			var subDirectories = directory.Directories
				// add a slash at the end to help differentiate between folder and file
				.Select(s => s + "/")
				.ToList();
			subDirectories.AddRange(directory.Files.Select(f => f.Name));

			// splitting into rows of 4 to simulate normal directory printing behavior
			for (var i = 0; i < Math.Ceiling(subDirectories.Count / 4.0); i++)
			{
				var columns = subDirectories.Skip(i * 4).Take(4);
				responseLines.Add(string.Join(Tab, columns));
			}

			return new CommandResult { ResponseLines = responseLines };
		}

		private CommandResult ExecuteReadFile(Command command, string currentPath)
		{
			var file = command.Arguments.FirstOrDefault();
			FileInfo fileInfo;
			if (string.IsNullOrWhiteSpace(file) || (fileInfo = this._directoryManager.GetFileInDirectory(currentPath, file)) == null)
			{
				return new CommandResult
				{
					ResponseLines = new List<string> { $"Cannot read invalid filename '{file}'" },
					Color = Color.Red
				};
			}

			if (Path.GetExtension(file) == ".pdf")
			{
				var urlForFile = $"{HttpContext.Current.Request.Url.Scheme}://{HttpContext.Current.Request.Url.Authority}/FileSystem{currentPath}/{file}";
				return new CommandResult
				{
					RedirectUrl = new Uri(urlForFile)
				};
			}

			var responseLines = new List<string>();
			responseLines.AddRange(fileInfo.Content.ToList());
			return new CommandResult
			{
				ResponseLines = responseLines
			};
		}

		private CommandResult ExecuteChangeDirectory(Command command, string currentPath)
		{
			var directory = command.Arguments.FirstOrDefault();

			// trim the end slash (if there) on dir since both are allowed in shell commands but we key without it
			var trimmedDirectory = directory?.TrimEnd('/');

			// special case recursion folder and remain in the same folder
			if (currentPath.Contains("recursion") && trimmedDirectory == "recursion")
			{
				return new CommandResult { NewPath = currentPath };
			}

			DirectoryInfo directoryInfo;
			if (string.IsNullOrWhiteSpace(directory) || !this._directoryManager.TryChangeDirectory(currentPath, trimmedDirectory, out directoryInfo))
			{
				return new CommandResult
				{
					ResponseLines = new List<string> { $"Cannot change directory from '{currentPath}' to '{directory}'" },
					Color = Color.Red
				};
			}

			return new CommandResult
			{
				// trim the front slash since we render that on the page already (for the empty case)
				NewPath = directoryInfo?.Name
			};
		}

		private CommandResult ExecuteRunShellScript(Command command, string currentPath)
		{
			var file = command.Arguments.FirstOrDefault();
			var directory = this._directoryManager.GetDirectoryInfo(currentPath);

			var fileInfo = directory.Files.SingleOrDefault(f => f.Name == file);
			if (fileInfo == null)
			{
				return new CommandResult
				{
					ResponseLines = new List<string> { $"Cannot execute invalid shell script '{file}'" }
				};
			}

			return FileExecutor.Execute(this._urlHelper, fileInfo);
		}

		private CommandResult ExecuteRunExecutable(Command command, string currentPath)
		{
			var file = command.Arguments.FirstOrDefault();
			var directory = this._directoryManager.GetDirectoryInfo(currentPath);

			var fileInfo = directory.Files.SingleOrDefault(f => f.Name == file);
			if (fileInfo == null)
			{
				return new CommandResult
				{
					ResponseLines = new List<string> { $"Cannot execute invalid executable file '{file}'" }
				};
			}

			return FileExecutor.Execute(this._urlHelper, fileInfo);
		}

		private static TwoWayMap<string, CommandType> PopulateCommandMap()
		{
			var map = new TwoWayMap<string, CommandType>();
			map.Add("ls", CommandType.GetDirectoryContents);
			map.Add("cd", CommandType.ChangeDirectories);
			map.Add("pwd", CommandType.PrintCurrentDirectory);
			map.Add("cat", CommandType.ReadFile);
			map.Add("sh", CommandType.RunShellScript);
			map.Add("./", CommandType.RunExecutable);
			map.Add("help", CommandType.Help);
			map.Add("exit", CommandType.Exit);
			return map;
		} 

		private static string GetCommandDescription(CommandType type)
		{
			switch (type)
			{
				case CommandType.GetDirectoryContents:
					return $"{CommandMap.Reverse[type]} - lists the contents of the current directory";
				case CommandType.ReadFile:
					return $"{CommandMap.Reverse[type]} [filename] - prints the specified file's content";
				case CommandType.ChangeDirectories:
					return $"{CommandMap.Reverse[type]} [path] - changes the current directory to the specified path";
				case CommandType.PrintCurrentDirectory:
					return $"{CommandMap.Reverse[type]} - prints the current directory path";
				case CommandType.RunShellScript:
					return $"{CommandMap.Reverse[type]} [shellFileName.sh] - executes the specified shell script";
				case CommandType.RunExecutable:
					return $"{CommandMap.Reverse[type]}[executableFileName] - executes the specified executable file";
				case CommandType.Help:
					return $"{CommandMap.Reverse[type]} - lists terminal commands";
				case CommandType.Exit:
					return $"{CommandMap.Reverse[type]} - exits the terminal";
				default:
					return null;
			}
		}
	}

	public class Command
	{
		public CommandType Type { get; set; }
		public string CommandString { get; set; }
		public List<string> Arguments { get; set; }
	}

	public class CommandResult
	{
		public List<string> ResponseLines { get; set; }
		public string NewPath { get; set; }
		public Color? Color { get; set; }
		public string ColorCode
		{
			get
			{
				if (this.Color.HasValue)
				{
					var c = this.Color.Value;
					return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
				}
				else
				{
					return null;
				}
			}
		}
		public Uri RedirectUrl { get; set; }
		public Uri PromptResponsePostUrl { get; set; }
		public string JavaScriptCode { get; set; }
		public object Metadata { get; set; }
	}

	public enum CommandType
	{
		Invalid = 0,
		GetDirectoryContents = 1,
		ChangeDirectories = 2,
		PrintCurrentDirectory = 3,
		ReadFile = 4,
		RunShellScript = 5,
		RunExecutable = 7,
		Help = 8,
		Exit = 9
	}
}

			