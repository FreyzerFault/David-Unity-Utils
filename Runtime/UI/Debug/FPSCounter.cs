using TMPro;
using UnityEngine;

namespace DavidUtils.UI.Debug
{
	[RequireComponent(typeof(TMP_Text))]
	public class FPSCounter : MonoBehaviour
	{
		private const float FpsMeasurePeriod = 0.5f;
		private int _m_FpsAccumulator;
		private float _m_FpsNextPeriod;
		private int _m_CurrentFps;
		private const string Display = "{0} FPS";
		private TMP_Text _m_Text;

		private void Start()
		{
			_m_FpsNextPeriod = Time.realtimeSinceStartup + FpsMeasurePeriod;
			_m_Text = GetComponent<TMP_Text>();
		}

		private void Update()
		{
			// measure average frames per second
			_m_FpsAccumulator++;
			if (Time.realtimeSinceStartup > _m_FpsNextPeriod)
			{
				_m_CurrentFps = (int)(_m_FpsAccumulator / FpsMeasurePeriod);
				_m_FpsAccumulator = 0;
				_m_FpsNextPeriod += FpsMeasurePeriod;
				_m_Text.text = string.Format(Display, _m_CurrentFps);
			}
		}
	}
}
