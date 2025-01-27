using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CentralAuth;
using Compendium.Npc;
using helpers;
using helpers.Events;
using helpers.Extensions;
using helpers.Pooling;
using helpers.Random;
using helpers.Values;
using MEC;
using NVorbis;
using UnityEngine;
using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Networking;
using YoutubeExplode.Videos;

namespace Compendium.Sounds;

public class AudioPlayer : Poolable
{
	internal ReferenceHub _speaker;

	private OpusEncoder _encoder;

	private VorbisReader _reader;

	private MemoryStream _stream;

	private AudioData _current;

	private AudioData _next;

	private PlaybackBuffer _playbackBuffer;

	private Queue<float> _buffer;

	private ConcurrentQueue<string> _queryQueue;

	private ConcurrentQueue<AudioData> _dataQueue;

	private HashSet<uint> _whitelist;

	private HashSet<uint> _blacklist;

	private CoroutineHandle _coroutine;

	private byte[] _encodedBuffer;

	private float[] _sendBuffer;

	private float[] _readBuffer;

	private float _maxSamples;

	private bool _updateReg;

	private bool _sMoved;

	public AudioStatus Status { get; private set; } = AudioStatus.Idle;


	public VoiceChatChannel Channel { get; set; } = VoiceChatChannel.Proximity;


	public VoiceChatChannel ChannelMode { get; set; }

	public HistoryValue<Vector3?> Position { get; } = new HistoryValue<Vector3?>();


	public string Name { get; set; } = "default";


	public float Volume { get; set; } = 100f;


	public float Distance { get; set; } = 20f;


	public bool IsLooping { get; set; }

	public bool IsReady { get; set; }

	public bool IsPaused { get; set; }

	public bool ShouldContinue { get; set; } = true;


	public bool ShouldStopTrack { get; set; }

	public bool SendToSelf { get; set; }

	public EventProvider OnSelectingTrack { get; } = new EventProvider();


	public EventProvider OnSelectedTrack { get; } = new EventProvider();


	public EventProvider OnLoadingTrack { get; } = new EventProvider();


	public EventProvider OnLoadedTrack { get; } = new EventProvider();


	public EventProvider OnFinishedTrack { get; } = new EventProvider();


	public EventProvider OnStartedPlayback { get; } = new EventProvider();


	public EventProvider OnStoppedPlayback { get; } = new EventProvider();


	public bool IsWhitelisted(ReferenceHub hub)
	{
		return _whitelist.Contains(hub.netId);
	}

	public void AddWhitelist(ReferenceHub hub)
	{
		_whitelist.Add(hub.netId);
	}

	public void RemoveWhitelist(ReferenceHub hub)
	{
		_whitelist.Remove(hub.netId);
	}

	public void ClearWhitelist()
	{
		_whitelist.Clear();
	}

	public bool IsBlacklisted(ReferenceHub hub)
	{
		return _blacklist.Contains(hub.netId);
	}

	public void AddBlacklist(ReferenceHub hub)
	{
		_blacklist.Add(hub.netId);
	}

	public void RemoveBlacklist(ReferenceHub hub)
	{
		_blacklist.Remove(hub.netId);
	}

	public void ClearBlacklist()
	{
		_blacklist.Clear();
	}

	public void Pause()
	{
		IsPaused = true;
	}

	public void Resume()
	{
		IsPaused = false;
	}

	public void Stop(bool clear = true)
	{
		_speaker?.RemoveAllFakePositions();
		if (clear)
		{
			Collections.Clear(_dataQueue);
			Collections.Clear(_queryQueue);
		}
		ShouldStopTrack = true;
	}

	public void Queue(string query, Action<string> message)
	{
		message?.Invoke("Attempting to find '" + query + "' in manifest history ..");
		if (AudioStore.TryGet(query.RemovePathUnsafe(), out var data))
		{
			Queue(data, message, convOverride: false);
			message?.Invoke($"Retrieved {data.Length} bytes from the manifest!");
			return;
		}
		message?.Invoke("Searching on YouTube ..");
		AudioSearch.Find(query, message, delegate(VideoId vid)
		{
			if (!string.IsNullOrWhiteSpace(vid.Value))
			{
				if (AudioStore.TryGet(vid.Value, out data))
				{
					Queue(data, message, convOverride: false);
					message?.Invoke("Found video ID '" + vid.Value + "' in the manifest!");
				}
				else
				{
					message?.Invoke("Downloading video ID '" + vid.Value + "' ..");
					AudioSearch.Download(vid, message, delegate(byte[] newData)
					{
						Queue(new AudioData("pipe-web", query.RemovePathUnsafe(), newData), message);
					});
				}
			}
		});
	}

	public void Queue(AudioData data, Action<string> message, bool convOverride = true)
	{
		try
		{
			if (data == null || data.Data == null)
			{
				return;
			}
			if (convOverride || (data.RequiresConversion && !convOverride))
			{
				AudioConverter.Convert(data.Data, message, delegate(byte[] newData)
				{
					if (newData != null)
					{
						data.Data = newData;
						if (data.Source != "pipe-source" && !AudioStore.TryGet(data.Id, out var _))
						{
							AudioStore.Save(data.Id, data.Data);
						}
						if (!Timing.IsRunning(_coroutine))
						{
							_current = data;
							_coroutine = Timing.RunCoroutine(PlaybackHandler());
							message?.Invoke("Starting the playback coroutine ..");
						}
						else
						{
							if (_next == null)
							{
								_next = data;
							}
							else
							{
								_dataQueue.Enqueue(data);
							}
							message?.Invoke("Queued audio.");
						}
					}
				});
				return;
			}
			if (data.Source != "pipe-source" && !AudioStore.TryGet(data.Id, out var _))
			{
				AudioStore.Save(data.Id, data.Data);
			}
			if (!Timing.IsRunning(_coroutine))
			{
				_current = data;
				_coroutine = Timing.RunCoroutine(PlaybackHandler());
				message?.Invoke("Starting the playback coroutine ..");
				return;
			}
			if (_next == null)
			{
				_next = data;
			}
			else
			{
				_dataQueue.Enqueue(data);
			}
			message?.Invoke("Queued audio.");
		}
		catch (Exception message2)
		{
			Plugin.Error(message2);
		}
	}

	public void Queue(byte[] data, Action<string> message, bool convOverride = true)
	{
		Queue(new AudioData("pipe-source", RandomGeneration.Default.GetReadableString(5), data, convOverride), message, convOverride);
	}

	public override void OnPooled()
	{
		base.OnPooled();
		Stop();
		if (_speaker != null && NpcHub.TryGetNpc(_speaker, out var npc))
		{
			NpcPool.Pool.Push(npc);
		}
		_speaker?.RemoveAllFakePositions();
		_speaker = null;
		_sMoved = false;
		Audio._activePlayers.Remove(this);
		if (Timing.IsRunning(_coroutine))
		{
			Timing.KillCoroutines(_coroutine);
		}
		_encoder.Dispose();
		_encoder = null;
		_playbackBuffer.Dispose();
		_playbackBuffer = null;
		_buffer.Clear();
		_buffer = null;
		Collections.Clear(_queryQueue);
		Collections.Clear(_dataQueue);
		_queryQueue = null;
		_dataQueue = null;
		_whitelist.Clear();
		_whitelist = null;
		_blacklist.Clear();
		_blacklist = null;
		_maxSamples = 0f;
		_stream?.Dispose();
		_stream = null;
		_reader?.Dispose();
		_reader = null;
		_current = null;
		_next = null;
		_speaker = null;
		_readBuffer = null;
		_sendBuffer = null;
		_encodedBuffer = null;
		Status = AudioStatus.Idle;
		Position.Reset();
		OnFinishedTrack.UnregisterAll();
		OnLoadedTrack.UnregisterAll();
		OnLoadingTrack.UnregisterAll();
		OnSelectedTrack.UnregisterAll();
		OnSelectingTrack.UnregisterAll();
		OnStartedPlayback.UnregisterAll();
		OnStoppedPlayback.UnregisterAll();
		Plugin.Info("Pooled player '" + Name + "'");
	}

	public override void OnUnpooled()
	{
		base.OnUnpooled();
		_encoder = new OpusEncoder(Plugin.Config.ApiSetttings.AudioSettings.OpusType);
		_playbackBuffer = new PlaybackBuffer();
		_buffer = new Queue<float>();
		_queryQueue = new ConcurrentQueue<string>();
		_dataQueue = new ConcurrentQueue<AudioData>();
		_whitelist = new HashSet<uint>();
		_blacklist = new HashSet<uint>();
		_encodedBuffer = new byte[Plugin.Config.ApiSetttings.AudioSettings.EncodingBufferSize];
		_maxSamples = 0f;
		if (!_updateReg)
		{
			_updateReg = typeof(StaticUnityMethods).TryAddHandler<Action>("OnUpdate", UpdateHandler);
		}
		Audio._activePlayers.Add(this);
		Plugin.Info("Unpooled player '" + Name + "'");
	}

	private IEnumerator<float> PlaybackHandler()
	{
		_next = null;
		ShouldStopTrack = false;
		if (IsLooping)
		{
			_next = _current;
		}
		else
		{
			_dataQueue.TryDequeue(out _next);
		}
		_stream = new MemoryStream(_current.Data);
		_reader = new VorbisReader(_stream);
		OnLoadingTrack.Invoke(_current);
		if (_reader.Channels != 1)
		{
			Plugin.Error("Failed to start playback (" + _current.Id + "): Audio must be mono.");
			yield return Timing.WaitForSeconds(1f);
			if (_next != null)
			{
				_coroutine = Timing.RunCoroutine(PlaybackHandler());
			}
			_reader.Dispose();
			_stream.Dispose();
			_reader = null;
			_stream = null;
		}
		if (_reader.SampleRate != 48000)
		{
			Plugin.Error("Failed to start playback (" + _current.Id + "): Audio must have a sampling rate of 48 000 Hz.");
			yield return Timing.WaitForSeconds(1f);
			if (_next != null)
			{
				_coroutine = Timing.RunCoroutine(PlaybackHandler());
			}
			_reader.Dispose();
			_stream.Dispose();
			_reader = null;
			_stream = null;
		}
		OnLoadedTrack.Invoke(_current);
		_sendBuffer = new float[Plugin.Config.ApiSetttings.AudioSettings.SendBufferSize];
		_readBuffer = new float[Plugin.Config.ApiSetttings.AudioSettings.ReadBufferSize];
		while (_reader.ReadSamples(_readBuffer, 0, _readBuffer.Length) > 0)
		{
			if (ShouldStopTrack)
			{
				_reader.SeekTo(_reader.TotalSamples - 1);
				ShouldStopTrack = false;
				Status = AudioStatus.Stopped;
			}
			while (IsPaused)
			{
				Status = AudioStatus.Paused;
				yield return float.NegativeInfinity;
			}
			while (_buffer.Count >= _readBuffer.Length)
			{
				IsReady = true;
				Status = AudioStatus.Playing;
				yield return float.NegativeInfinity;
			}
			for (int i = 0; i < _readBuffer.Length; i++)
			{
				_buffer.Enqueue(_readBuffer[i]);
			}
			Status = AudioStatus.Playing;
		}
		OnFinishedTrack.Invoke(_current);
		Status = AudioStatus.Idle;
		yield return Timing.WaitForSeconds(1f);
		if (IsLooping)
		{
			_next = _current;
		}
		if (_next == null)
		{
			_dataQueue.TryDequeue(out _next);
		}
		if (_next != null)
		{
			_coroutine = Timing.RunCoroutine(PlaybackHandler());
		}
		_reader.Dispose();
		_stream.Dispose();
		_reader = null;
		_stream = null;
	}

	private void UpdateHandler()
	{
		if (!IsReady || _buffer.IsEmpty() || IsPaused)
		{
			return;
		}
		_maxSamples += Time.deltaTime * (float)Plugin.Config.ApiSetttings.AudioSettings.SamplingRate;
		int num = Mathf.Min(Mathf.FloorToInt(_maxSamples), _buffer.Count);
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				_playbackBuffer.Write(_buffer.Dequeue() * (Volume / 100f));
			}
		}
		_maxSamples -= num;
		while (_playbackBuffer.Length >= 480)
		{
			_playbackBuffer.ReadTo(_sendBuffer, 480L, 0L);
			int size = _encoder.Encode(_sendBuffer, _encodedBuffer);
			Hub.Hubs.ForEach(delegate(ReferenceHub hub)
			{
				if (hub.Mode == ClientInstanceMode.ReadyClient && (_speaker.netId == hub.netId || ((!_whitelist.Any() || _whitelist.Contains(hub.netId)) && !_blacklist.Contains(hub.netId) && AudioUtils.ValidateChannelMode(Channel, ChannelMode, hub, _speaker, Distance))) && (!Audio.Mutes.TryGetFirst((KeyValuePair<string, HashSet<string>> x) => x.Key == Name, out var value) || !value.Value.Contains(hub.UserId())))
				{
					if (_speaker.netId == hub.netId)
					{
						if (!SendToSelf)
						{
							return;
						}
						if (Channel == VoiceChatChannel.Proximity)
						{
							hub.connectionToClient.Send(new VoiceMessage(_speaker, VoiceChatChannel.RoundSummary, _encodedBuffer, size, isNull: false));
							return;
						}
					}
					hub.connectionToClient.Send(new VoiceMessage(_speaker, Channel, _encodedBuffer, size, isNull: false));
				}
			});
		}
	}
}
