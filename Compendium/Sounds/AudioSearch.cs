using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using helpers.Extensions;
using helpers.Random;
using helpers.Time;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Compendium.Sounds;

public static class AudioSearch
{
	private static YoutubeClient _yt = new YoutubeClient();

	public static void Find(string query, Action<string> message, Action<VideoId> callback)
	{
		new Thread((ThreadStart)async delegate
		{
			message?.Invoke("Searching for query: '" + query + "'");
			foreach (ISearchResult item in await _yt.Search.GetResultsAsync(query).CollectAsync())
			{
				if (item is VideoSearchResult videoSearchResult)
				{
					message?.Invoke($"Found result: '{videoSearchResult.Title}' (by '{videoSearchResult.Author}') [{videoSearchResult.Duration.GetValueOrDefault().UserFriendlySpan()}]");
					callback?.Invoke(videoSearchResult.Id);
					return;
				}
			}
			callback?.Invoke(default(VideoId));
			message?.Invoke("Failed to find any results for your query!");
		}).Start();
	}

	public static void Download(VideoId video, Action<string> message, Action<byte[]> result)
	{
		new Thread((ThreadStart)async delegate
		{
			try
			{
				message?.Invoke("Retrieving streaming manifest ..");
				IEnumerable<AudioOnlyStreamInfo> audioOnlyStreams = (await _yt.Videos.Streams.GetManifestAsync(video)).GetAudioOnlyStreams();
				message?.Invoke($"Found {audioOnlyStreams.Count()} audio stream(s).");
				if (!audioOnlyStreams.Any())
				{
					message?.Invoke("Failed to find a valid audio stream!");
					result?.Invoke(null);
				}
				else
				{
					AudioOnlyStreamInfo audioOnlyStreamInfo = audioOnlyStreams.OrderByDescending((AudioOnlyStreamInfo a) => a.Bitrate.BitsPerSecond).First();
					string tempPath = string.Concat(str2: RandomGeneration.Default.GetReadableString(20).RemovePathUnsafe().Replace("/", ""), str0: AudioStore.DirectoryPath, str1: "/");
					message?.Invoke($"Selected audio stream: {audioOnlyStreamInfo.AudioCodec} ({audioOnlyStreamInfo.Bitrate.BitsPerSecond} b/s)");
					message?.Invoke("Downloading ..");
					await _yt.Videos.Streams.DownloadAsync(audioOnlyStreamInfo, tempPath);
					byte[] array = File.ReadAllBytes(tempPath);
					File.Delete(tempPath);
					message?.Invoke($"Downloaded {array.Length} bytes!");
					result?.Invoke(array);
				}
			}
			catch (Exception ex)
			{
				Exception message2 = ex;
				Plugin.Error(message2);
			}
		}).Start();
	}
}
