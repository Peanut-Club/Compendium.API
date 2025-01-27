using System;
using System.Reflection;
using Compendium.Comparison;

namespace Compendium.Updating;

public class UpdateData
{
	public UpdateCall CallType { get; }

	public DateTime LastCallTime { get; internal set; } = DateTime.Now;


	public int DelayTime { get; set; } = -1;


	public bool IsMeasured { get; set; }

	public bool IsEverMeasured { get; private set; }

	public bool IsUnity { get; } = true;


	public bool PauseWaiting { get; } = true;


	public bool PauseRestarting { get; } = true;


	public double LastCall { get; set; }

	public double LongestCall { get; set; }

	public double ShortestCall { get; set; }

	public double AverageCall => (LongestCall + ShortestCall) / 2.0;

	public Action ParameterlessCall { get; }

	public Action<UpdateData> ParameterCall { get; }

	public UpdateData(bool isUnity, bool isWaiting, bool isRestarting, int delayTime, Action parameterlessCall)
	{
		IsUnity = isUnity;
		PauseWaiting = isWaiting;
		PauseRestarting = isRestarting;
		CallType = UpdateCall.WithoutParameter;
		DelayTime = delayTime;
		ParameterlessCall = parameterlessCall;
	}

	public UpdateData(bool isUnity, bool isWaiting, bool isRestarting, int delayTime, Action<UpdateData> parameterCall)
	{
		IsUnity = isUnity;
		PauseWaiting = isWaiting;
		PauseRestarting = isRestarting;
		CallType = UpdateCall.WithParameter;
		DelayTime = delayTime;
		ParameterCall = parameterCall;
	}

	public bool CanRun()
	{
		if (DelayTime > 0)
		{
			return (DateTime.Now - LastCallTime).TotalMilliseconds >= (double)DelayTime;
		}
		return true;
	}

	public bool Is(MethodBase method, object target)
	{
		if (ParameterCall != null)
		{
			if (ParameterCall.Method == method)
			{
				return NullableObjectComparison.Compare(target, ParameterCall.Target);
			}
			return false;
		}
		if (ParameterlessCall != null)
		{
			if (ParameterlessCall.Method == method)
			{
				return NullableObjectComparison.Compare(target, ParameterlessCall.Target);
			}
			return false;
		}
		return false;
	}

	public void DoCall()
	{
		if (!CanRun())
		{
			return;
		}
		LastCallTime = DateTime.Now;
		try
		{
			if (IsMeasured)
			{
				DateTime now = DateTime.Now;
				if (CallType == UpdateCall.WithoutParameter)
				{
					ParameterlessCall();
				}
				else
				{
					ParameterCall(this);
				}
				double num = (LastCall = (DateTime.Now - now).TotalMilliseconds);
				double num2 = num;
				if (LastCall > LongestCall)
				{
					LongestCall = num2;
				}
				if (LastCall < ShortestCall)
				{
					ShortestCall = num2;
				}
			}
			else if (CallType == UpdateCall.WithoutParameter)
			{
				ParameterlessCall();
			}
			else
			{
				ParameterCall(this);
			}
		}
		catch (Exception message)
		{
			Plugin.Error("Failed to invoke update");
			Plugin.Error(message);
		}
	}
}
