using System;
using System.IO;

namespace Compendium.IO.Watcher;

public class Watcher
{
	private string _file;

	private bool _recent;

	public bool IsRecent
	{
		get
		{
			return _recent;
		}
		set
		{
			if (_recent != value && value)
			{
				_recent = value;
				Calls.Delay(1f, delegate
				{
					_recent = false;
				});
			}
		}
	}

	public event Action OnFileChanged;

	public Watcher(string path)
	{
		_file = Path.GetFullPath(path);
		FileSystemWatcher fileSystemWatcher = new FileSystemWatcher
		{
			Path = Path.GetDirectoryName(path),
			NotifyFilter = NotifyFilters.LastWrite
		};
		fileSystemWatcher.Changed += OnChanged;
		fileSystemWatcher.EnableRaisingEvents = true;
	}

	private void OnChanged(object source, FileSystemEventArgs e)
	{
		if (!(e.FullPath != _file) && !_recent)
		{
			IsRecent = true;
			this.OnFileChanged?.Invoke();
		}
	}
}
