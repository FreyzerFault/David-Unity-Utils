using System;
using UnityEngine;

namespace DavidUtils.DevTools.Testing
{
	public static class DebugTimer
	{
		public static void DebugTime(Action action, string message = "Time to run")
		{
			float iniTime = Time.realtimeSinceStartup;
			action();
			float endTime = Time.realtimeSinceStartup;
			float time = (endTime - iniTime) * 1000;
			string color = time < 10 ? "cyan" : time < 30 ? "green" : time < 60 ? "yellow" : "red";
			Debug.Log($"<b><color=white>{message}: <b><color={color}>{time:F0} ms</color></b></color></b>");
		}

		public static float RunTimerInMs(Action action)
		{
			float iniTime = Time.realtimeSinceStartup;
			action();
			float endTime = Time.realtimeSinceStartup;
			return (endTime - iniTime) * 1000;
		}
	}
}
