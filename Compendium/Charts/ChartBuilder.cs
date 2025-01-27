using System.Collections.Generic;
using System.Text.Json;
using QuickChart;

namespace Compendium.Charts;

public static class ChartBuilder
{
	public static byte[] GetChart(string label, IEnumerable<KeyValuePair<string, int>> data)
	{
		return BuildHorizontalBarChart(label, data).ToByteArray();
	}

	public static QuickChart.Chart BuildHorizontalBarChart(string label, IEnumerable<KeyValuePair<string, int>> data)
	{
		return BuildChart("horizontalBar", label, data);
	}

	public static QuickChart.Chart BuildChart(string type, string label, IEnumerable<KeyValuePair<string, int>> data)
	{
		Chart chart = new Chart();
		ChartData chartData = new ChartData();
		ChartDataset chartDataset = new ChartDataset();
		chart.Type = type;
		List<string> list = new List<string>();
		List<int> list2 = new List<int>();
		foreach (KeyValuePair<string, int> datum in data)
		{
			list.Add(datum.Key);
			list2.Add(datum.Value);
		}
		chartDataset.Label = label;
		chartDataset.Data = list2.ToArray();
		chartData.Labels = list.ToArray();
		chartData.Datasets = new ChartDataset[1] { chartDataset };
		chart.Data = chartData;
		string config = JsonSerializer.Serialize(chart);
		QuickChart.Chart chart2 = new QuickChart.Chart();
		chart2.Width = 1440;
		chart2.Height = 1024;
		chart2.Version = "2.9.4";
		chart2.Config = config;
		return chart2;
	}
}
