using System.Collections.Generic;
using System.Linq;

namespace Compendium.Events;

public class EventStatistics
{
	private List<double> _average = new List<double>();

	public double LongestTime { get; set; } = -1.0;


	public double ShortestTime { get; set; } = -1.0;


	public double AverageTime { get; set; } = -1.0;


	public double LastTime { get; set; } = -1.0;


	public double TicksWhenLongest { get; set; }

	public int Executions { get; set; }

	public void Reset()
	{
		_average.Clear();
		LongestTime = -1.0;
		ShortestTime = -1.0;
		AverageTime = -1.0;
		LastTime = -1.0;
		TicksWhenLongest = 0.0;
		Executions = 0;
	}

	public void Record(double time)
	{
		Executions++;
		LastTime = time;
		if (LongestTime == -1.0 || time > LongestTime)
		{
			LongestTime = time;
			TicksWhenLongest = World.TicksPerSecondFull;
		}
		if (ShortestTime == -1.0 || time < ShortestTime)
		{
			ShortestTime = time;
		}
		if (AverageTime == -1.0)
		{
			AverageTime = time;
		}
		_average.Add(time);
		if (_average.Count >= 10)
		{
			AverageTime = _average.Average();
			_average.Clear();
		}
	}

	public override string ToString()
	{
		return $"Longest: {LongestTime} ms\n" + $"Shortest: {ShortestTime} ms\n" + $"Last: {LastTime} ms\n" + $"Average: {AverageTime} ms\n" + $"Ticks When Highest: {TicksWhenLongest} TPS\n" + $"Total Executions: {Executions}";
	}
}
