using System;
using Microsoft.Extensions.Logging;

namespace Compendium.Logging;

public class LoggingProvider : ILoggerProvider, IDisposable
{
	public ILogger CreateLogger(string categoryName)
	{
		return new Logger(categoryName);
	}

	public void Dispose()
	{
	}
}
