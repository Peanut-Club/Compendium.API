using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Compendium.Logging;
using Grapevine;
using helpers;
using helpers.Attributes;
using helpers.Random;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Compendium.HttpServer;

public static class HttpController
{
	internal static volatile IRestServer _server;

	private static Thread _thread;

	private static CancellationTokenSource _cts;

	private static CancellationToken _ct;

	private static IConfiguration DefaultConfig { get; } = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();


	[Load]
	public static void Load()
	{
		try
		{
			if (Plugin.Config.ApiSetttings.HttpSettings.ServerPrefix == "none")
			{
				return;
			}
			ServiceCollection services = new ServiceCollection();
			services.AddSingleton(typeof(IConfiguration), DefaultConfig);
			services.AddSingleton<IRestServer, RestServer>();
			services.AddSingleton<IRouter, Router>();
			services.AddSingleton<IRouteScanner, RouteScanner>();
			services.AddTransient<IContentFolder, ContentFolder>();
			services.AddLogging(delegate(ILoggingBuilder b)
			{
				b.AddProvider(new LoggingProvider());
			});
			services.Configure(delegate(LoggerFilterOptions options)
			{
				options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
			});
			ServiceProvider provider = services.BuildServiceProvider();
			IRestServer server = provider.GetService<IRestServer>();
			server.Router.Services = services;
			server.RouteScanner.Services = services;
			AssemblyName name = typeof(HttpController).Assembly.GetName();
			server.GlobalResponseHeaders.Add("Server", $"{name.Name}/{name.Version} ({RuntimeInformation.OSDescription})");
			server.Prefixes.Add(Plugin.Config.ApiSetttings.HttpSettings.ServerPrefix);
			services.AddSingleton(server);
			services.AddSingleton(server.Router);
			services.AddSingleton(server.RouteScanner);
			server.SetDefaultLogger(new LoggingFactory());
			_cts = new CancellationTokenSource();
			_ct = _cts.Token;
			_server = server;
			_thread = new Thread((ThreadStart)async delegate
			{
				server.Start();
				while (!_ct.IsCancellationRequested)
				{
					await Task.Delay(150);
				}
			});
			_thread.Priority = ThreadPriority.AboveNormal;
			_thread.Start();
		}
		catch (Exception message)
		{
			Plugin.Error(message);
		}
	}

	public static string AddRoute(Func<IHttpContext, Task> routeHandler, HttpMethod method, string pattern)
	{
		string readableString = RandomGeneration.Default.GetReadableString(60);
		Route route = new Route(routeHandler, method, pattern, enabled: true, readableString, readableString);
		_server.Router.Register(route);
		return readableString;
	}

	public static void AddRoutes<T>()
	{
		AddRoutes(typeof(T));
	}

	public static void AddRoutes(Type type)
	{
		if (_server == null)
		{
			Calls.OnFalse(delegate
			{
				IList<IRoute> list2 = _server.RouteScanner.Scan(type);
				if (list2 != null && list2.Any())
				{
					_server.Router.Register(list2);
				}
			}, () => _server == null);
		}
		else
		{
			IList<IRoute> list = _server.RouteScanner.Scan(type);
			if (list != null && list.Any())
			{
				_server.Router.Register(list);
			}
		}
	}

	public static void RemoveRoute(string id)
	{
		if (_server.Router.RoutingTable.TryGetFirst((IRoute x) => x.Name == id, out var value))
		{
			value.Disable();
			_server.Router.RoutingTable.Remove(value);
		}
	}

	public static void RemoveRoutes<T>()
	{
		RemoveRoutes(typeof(T));
	}

	public static void RemoveRoutes(Type type)
	{
		if (_server == null)
		{
			return;
		}
		IList<IRoute> list = _server.RouteScanner.Scan(type);
		if (list == null || !list.Any())
		{
			return;
		}
		list.ForEach(delegate(IRoute x)
		{
			if (_server.Router.RoutingTable.TryGetFirst((IRoute y) => y.Equals(x), out var value))
			{
				value.Disable();
				_server.Router.RoutingTable.Remove(value);
			}
		});
	}

	[Unload]
	public static void Stop()
	{
		_cts.Cancel();
		_server.Stop();
		_server = null;
	}
}
