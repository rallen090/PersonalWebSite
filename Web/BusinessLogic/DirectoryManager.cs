using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Web
{
	public class DirectoryManager
	{
		private const string FileSystemDirectoryName = "FileSystem";
		private readonly Dictionary<string, DirectoryInfo> DirectoryMappings;

		public DirectoryManager()
		{
			DirectoryMappings = Initialize();
		}

		private static Dictionary<string, DirectoryInfo> Initialize()
		{
			var root = HttpRuntime.AppDomainAppPath;
			var fileSystemDirectory = Directory.GetDirectories(root, FileSystemDirectoryName, SearchOption.TopDirectoryOnly).SingleOrDefault();

			if (fileSystemDirectory == null)
			{
				throw new Exception($"Cannot locate the file system at {root}");
			}

			var directories = Directory.EnumerateDirectories(fileSystemDirectory, "*", SearchOption.AllDirectories)
				.Select(d => new DirectoryInfo
				{
					NameWithPath = d,
					Name = new System.IO.DirectoryInfo(d).FullName.Replace(fileSystemDirectory, string.Empty).Replace('\\', '/'),
					Directories = Directory.GetDirectories(d).Select(di => new System.IO.DirectoryInfo(di).Name).ToList(),
					Files = Directory.GetFiles(d).Select(f => new FileInfo { Name = Path.GetFileName(f), Content = File.ReadAllLines(f).ToList() }).ToList()
				})
				.ToList();
			directories.Add(new DirectoryInfo
			{
				NameWithPath = fileSystemDirectory,
				Name = string.Empty,
				Directories = Directory.GetDirectories(fileSystemDirectory).Select(d => new System.IO.DirectoryInfo(d).Name).ToList(),
				Files = Directory.GetFiles(fileSystemDirectory).Select(f => new FileInfo { Name = Path.GetFileName(f), Content = File.ReadAllLines(f).ToList() }).ToList()
			});
			var directoryInfo = new Dictionary<string, DirectoryInfo>();
			directories.ForEach(d => directoryInfo.Add(d.Name, d));
			return directoryInfo;
		}

		public bool TryChangeDirectory(string currentDirectory, string path, out DirectoryInfo directoryInfo)
		{
			// exact path
			if (DirectoryMappings.TryGetValue(path, out directoryInfo))
			{
				return true;
			}

			// relative path
			var currentPathSections = currentDirectory.Split('/').ToList();
			foreach (var section in path.Split('/'))
			{
				if (section == "..")
				{
					currentPathSections = currentPathSections.Take(currentPathSections.Count - 1).ToList();
				}
				else
				{
					var currentDirectories = GetSubDirectories(string.Join("/", currentPathSections));
					if (currentDirectories.Contains(section))
					{
						currentPathSections.Add(section);
					}
					else
					{
						return false;
					}
				}
			}
			if (DirectoryMappings.TryGetValue(string.Join("/", currentPathSections), out directoryInfo))
			{
				return true;
			}

			var subDirectories = this.GetSubDirectories(currentDirectory);
			foreach (var dir in subDirectories)
			{
				var combinedPath = Path.Combine(path, dir).Replace('\\', '/');
				if (DirectoryMappings.TryGetValue(combinedPath, out directoryInfo))
				{
					return true;
				}
			}

			return false;
		}

		public List<string> GetSubDirectories(string currentDirectory)
		{
			var directory = this.GetDirectoryInfo(currentDirectory);
			return directory.Directories;
		}

		public DirectoryInfo GetDirectoryInfo(string currentDirectory)
		{
			return DirectoryMappings[currentDirectory];
		}

		public bool TryGetDirectoryInfo(string currentDirectory, out DirectoryInfo directoryInfo)
		{
			return DirectoryMappings.TryGetValue(currentDirectory, out directoryInfo);
		}

		public FileInfo GetFileInDirectory(string currentDirectory, string filename)
		{
			var directory = this.GetDirectoryInfo(currentDirectory);
			return directory.Files.SingleOrDefault(f => f.Name == filename);
		}
	}

	public class DirectoryInfo
	{
		public string NameWithPath { get; set; }
		public string Name { get; set; }
		public List<string> Directories { get; set; }
		public List<FileInfo> Files { get; set; }
	}

	public class FileInfo
	{
		public string Name { get; set; }
		public List<string> Content { get; set; }
	}
}