using System;
using System.Collections.Generic;
using BetterCommands;
using Compendium.API.Compendium.Settings.SSS;
using Compendium.Events;
using Compendium.IO.Saving;
using GameCore;
using helpers;
using helpers.Attributes;
using helpers.Events;
using helpers.Extensions;
using PluginAPI.Core;
using PluginAPI.Events;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace Compendium.Input;

public static class InputManager
{
	private static readonly HashSet<IInputHandler> _handlers = new HashSet<IInputHandler>();

	private static SaveFile<CollectionSaveData<InputBinding>> _binds;

	public const string SyncCmdBindingKey = "enable_sync_command_binding";

	public static readonly EventProvider OnKeyPressed = new EventProvider();

	public static readonly EventProvider OnKeyRegistered = new EventProvider();

	public static readonly EventProvider OnKeyUnregistered = new EventProvider();

	public static readonly EventProvider OnKeySynchronized = new EventProvider();

	public static bool IsEnabled { get; private set; }

	public static void Register<THandler>() where THandler : IInputHandler, new()
	{
		if (TryGetHandler<THandler>(out var _))
		{
			Plugin.Warn("Attempted to register an already existing input handler.");
		}
		else
		{
			var handler = new THandler();
			string label = string.IsNullOrWhiteSpace(handler.Label) ? handler.Id : handler.Label;
			var settings = new SSKeybindSetting(null, label, suggestedKey: handler.Key);

			_handlers.Add(handler);
			SSSManager.AddNewKeybind(settings, (hub, _) => handler.OnPressed(hub));
		}
	}

	public static void Unregister<THandler>() where THandler : IInputHandler, new()
	{
		if (_handlers.RemoveWhere((IInputHandler h) => h is THandler) > 0)
		{
			OnKeyUnregistered.Invoke(typeof(THandler));
		}
	}

    public static bool TryGetHandler(string actionId, out IInputHandler handler)
	{
		return _handlers.TryGetFirst((IInputHandler h) => h.Id == actionId, out handler);
	}

	public static bool TryGetHandler<THandler>(out THandler handler) where THandler : IInputHandler, new()
	{
		if (_handlers.TryGetFirst((IInputHandler h) => h is THandler, out var value) && value is THandler val)
		{
			handler = val;
			return true;
		}
		handler = default(THandler);
		return false;
	}

	public static KeyCode KeyFor(ReferenceHub hub, IInputHandler handler)
	{
		if (_binds == null)
		{
			return handler.Key;
		}
		if (_binds.Data.TryGetFirst((InputBinding bind) => bind.Id == handler.Id && bind.OwnerId == hub.UniqueId(), out var value))
		{
			return value.Key;
		}
		return handler.Key;
	}

	/*
	[Load]
	[Reload]
	private static void Initialize()
	{
		IsEnabled = ConfigFile.ServerConfig.GetBool("enable_sync_command_binding");
		if (!IsEnabled)
		{
			if (_binds != null)
			{
				_binds.Save();
				_binds = null;
			}
			Plugin.Warn("Synchronized binding is disabled. (set \"enable_sync_command_binding\" to true in the gameplay config to enable)");
		}
		else if (_binds == null)
		{
			_binds = new SaveFile<CollectionSaveData<InputBinding>>(Directories.GetDataPath("SavedPlayerBinds", "playerBinds"));
		}
		else
		{
			_binds.Load();
		}
	}

	private static void SyncPlayer(ReferenceHub hub)
	{
		SSSManager.
		_handlers.ForEach(delegate(IInputHandler handler)
		{
			KeyCode keyCode = KeyFor(hub, handler);
			hub.characterClassManager.TargetChangeCmdBinding(keyCode, ".input " + handler.Id);
			hub.Message($"[INPUT - DEBUG] Synchronized key bind: {handler.Id} on key {keyCode}");
			OnKeySynchronized.Invoke(hub, handler, keyCode);
		});
	}

	private static void ReceiveKey(ReferenceHub player, string actionId)
	{
		OnKeyPressed.Invoke(player, actionId);
		if (_handlers.TryGetFirst((IInputHandler h) => h.Id == actionId, out var value))
		{
			try
			{
				value.OnPressed(player);
			}
			catch (Exception ex)
			{
				Plugin.Error("Failed to execute key bind: " + actionId);
				Plugin.Error(ex);
				player.Message("Failed to execute key bind: " + actionId);
				player.Message(ex.Message);
			}
		}
	}

	[Command("input", new CommandType[] { CommandType.PlayerConsole })]
	[Description("Executes a key bind on the server.")]
	private static string OnInputCommand(Player sender, string actionId)
	{
		if (!IsEnabled)
		{
			return "Key binds are disabled on this server.";
		}
		ReceiveKey(sender.ReferenceHub, actionId);
		return "Keybind executed.";
	}

	[Command("inputsync", new CommandType[] { CommandType.PlayerConsole })]
	[Description("Synchronizes server-side keybinds.")]
	private static string OnSyncCommand(Player sender)
	{
		SyncPlayer(sender.ReferenceHub);
		return "Synchronized keybinds.";
	}

	[Command("rebind", new CommandType[] { CommandType.PlayerConsole })]
	[Description("Allows you to customize your key binds.")]
	private static string OnRebindCommand(ReferenceHub sender, string actionId, KeyCode newKey)
	{
		if (_binds.Data.TryGetFirst((InputBinding bind) => bind.Id == actionId && bind.OwnerId == sender.UniqueId(), out var value))
		{
			value.Key = newKey;
			_binds.Save();
			SyncPlayer(sender);
			return "Bind action " + actionId + " to key " + newKey.ToString().SpaceByPascalCase() + "!";
		}
		_binds.Data.Add(new InputBinding
		{
			Id = actionId,
			Key = newKey,
			OwnerId = sender.UniqueId()
		});
		_binds.Save();
		SyncPlayer(sender);
		return "Bind action " + actionId + " to key " + newKey.ToString().SpaceByPascalCase() + "!";
	}

	[Event]
	private static void OnJoined(PlayerJoinedEvent ev)
	{
		SyncPlayer(ev.Player.ReferenceHub);
	}
	*/
}
