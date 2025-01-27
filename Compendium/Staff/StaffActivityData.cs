using System;
using helpers.Time;

namespace Compendium.Staff;

public class StaffActivityData
{
	public string UserId { get; set; }

	public long Total { get; set; }

	public long TotalOverwatch { get; set; }

	public long TwoWeeks { get; set; }

	public long TwoWeeksOverwatch { get; set; }

	public DateTime TwoWeeksStart { get; set; } = TimeUtils.LocalTime;

}
