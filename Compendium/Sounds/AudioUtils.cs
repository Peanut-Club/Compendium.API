using System;
using System.IO;
using System.Net;
using System.Threading;
using Compendium.Extensions;
using VoiceChat;
using YoutubeExplode.Videos;

namespace Compendium.Sounds;

public static class AudioUtils
{
	public static bool ValidateChannelMode(VoiceChatChannel channel, VoiceChatChannel mode, ReferenceHub receiver, ReferenceHub speaker, float distance)
	{
		if (mode == VoiceChatChannel.Proximity)
		{
			return receiver.IsWithinDistance(speaker, distance);
		}
		return true;
	}

	public static void Download(string target, string id, bool isDirect, Action<bool> callback = null)
	{
		if (isDirect)
		{
			new Thread((ThreadStart)async delegate
			{
				string path = Path.GetRandomFileName();
				using WebClient web = new WebClient();
				await web.DownloadFileTaskAsync(target, path);
				byte[] data = File.ReadAllBytes(path);
				File.Delete(path);
				AudioConverter.Convert(data, Plugin.Info, delegate(byte[] converted)
				{
					AudioStore.Save(id, converted);
					callback?.Invoke(obj: true);
				});
			}).Start();
			return;
		}
		AudioSearch.Find(target, Plugin.Info, delegate(VideoId vid)
		{
			if (string.IsNullOrWhiteSpace(vid.Value))
			{
				callback?.Invoke(obj: false);
			}
			else
			{
				AudioSearch.Download(vid, Plugin.Info, delegate(byte[] newData)
				{
					AudioConverter.Convert(newData, null, delegate(byte[] convertedData)
					{
						AudioStore.Save(id, convertedData);
						callback?.Invoke(obj: true);
					});
				});
			}
		});
	}
}
