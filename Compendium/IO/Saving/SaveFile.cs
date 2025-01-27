using System;
using System.IO;
using Compendium.IO.Watcher;

namespace Compendium.IO.Saving;

public class SaveFile<TData> where TData : SaveData, new()
{
	public DateTime SaveTime { get; private set; }

	public Compendium.IO.Watcher.Watcher Watcher { get; private set; }

	public TData Data { get; private set; }

	public string Path { get; }

	public bool IsUsingWatcher
	{
		get
		{
			return Watcher != null;
		}
		set
		{
			if ((Watcher != null || value) && !(Watcher != null && value))
			{
				if (value)
				{
					Watcher = new Compendium.IO.Watcher.Watcher(Path);
					Watcher.OnFileChanged += Load;
				}
				else if (Watcher != null)
				{
					Watcher.OnFileChanged -= Load;
					Watcher = null;
				}
			}
		}
	}

	public SaveFile(string path, bool useWatcher = true)
	{
		Path = path;
		IsUsingWatcher = useWatcher;
		Load();
	}

	public void Load()
	{
		if (Watcher != null && Watcher.IsRecent)
		{
			return;
		}
		if (!File.Exists(Path))
		{
			Save();
			return;
		}
		if (Data == null)
		{
			TData val2 = (Data = new TData());
			TData val3 = val2;
		}
		try
		{
			if (Data.IsBinary)
			{
				using FileStream input = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
				using BinaryReader reader = new BinaryReader(input);
				Data.Read(reader);
			}
			else
			{
				using FileStream stream = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
				using StreamReader reader2 = new StreamReader(stream);
				Data.Read(reader2);
			}
		}
		catch (Exception arg)
		{
			Plugin.Error($"Failed to load save file '{Path}' - the save data handler failed with an exception:\n{arg}");
		}
		SaveTime = DateTime.Now;
	}

	public void Save()
	{
		if (Data == null)
		{
			TData val2 = (Data = new TData());
			TData val3 = val2;
		}
		Watcher.IsRecent = true;
		try
		{
			if (!File.Exists(Path))
			{
				File.Create(Path).Close();
			}
			if (Data.IsBinary)
			{
				using FileStream output = new FileStream(Path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
				using BinaryWriter writer = new BinaryWriter(output);
				Data.Write(writer);
			}
			else
			{
				using FileStream stream = new FileStream(Path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
				using StreamWriter writer2 = new StreamWriter(stream);
				Data.Write(writer2);
			}
		}
		catch (Exception arg)
		{
			Plugin.Error($"Failed to save save file '{Path}' - the save data handler failed with an exception:\n{arg}");
		}
		SaveTime = DateTime.Now;
	}
}
