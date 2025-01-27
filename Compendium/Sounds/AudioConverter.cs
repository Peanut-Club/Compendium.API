using System;
using System.IO;
using System.Linq;
using System.Threading;
using helpers.Extensions;
using helpers.Random;
using Xabe.FFmpeg;

namespace Compendium.Sounds;

public static class AudioConverter
{
	public static void Convert(byte[] data, Action<string> message, Action<byte[]> result)
	{
		string text = RandomGeneration.Default.GetReadableString(20).RemovePathUnsafe().Replace("/", "");
		string sourcePath = AudioStore.DirectoryPath + "/" + text;
		string destPath = AudioStore.DirectoryPath + "/" + text + ".ogg";
		new Thread((ThreadStart)async delegate
		{
			try
			{
				File.WriteAllBytes(sourcePath, data);
				IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(sourcePath);
				message?.Invoke("Retrieving audio streams ..");
				IAudioStream audioStream = mediaInfo.AudioStreams.FirstOrDefault();
				message?.Invoke($"Chosen stream: {audioStream.Codec} '{audioStream.Bitrate} kb/s'");
				message?.Invoke("Converting ..");
				IConversion conversion = FFmpeg.Conversions.New().AddStream<IAudioStream>(audioStream).AddParameter("-vn")
					.AddParameter("-acodec libvorbis")
					.AddParameter("-ac 1")
					.AddParameter("-ar 48000")
					.AddParameter("-b:a 120k")
					.SetOutputFormat(Format.ogg)
					.SetOutput(destPath);
				await conversion.Start();
				byte[] obj = File.ReadAllBytes(destPath);
				message?.Invoke("Conversion finished!");
				File.Delete(sourcePath);
				File.Delete(destPath);
				result?.Invoke(obj);
			}
			catch (Exception ex)
			{
				Exception message2 = ex;
				Plugin.Error(message2);
			}
		}).Start();
		Plugin.Debug("Conversion thread " + text + " started");
	}
}
