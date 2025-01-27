using System;
using Microsoft.Extensions.Logging;

namespace Compendium.Logging;

public class LoggingFactory : ILoggerFactory, IDisposable
{
	public void AddProvider(ILoggerProvider provider)
	{
	}

	public void Dispose()
	{
	}

	public ILogger CreateLogger(string categoryName)
	{
		return new Logger(categoryName);
	}
}
