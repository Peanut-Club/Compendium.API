using System;
using helpers;
using Microsoft.Extensions.Logging;

namespace Compendium.Logging;

public class Logger : DisposableBase, ILogger
{
	public Logger(string source)
	{
	}

	public IDisposable BeginScope<TState>(TState state)
	{
		return this;
	}

	public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
	{
		return true;
	}

	public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
	{
	}
}
