using TMPro;
using UnityEngine;

namespace DavidUtils.UI.Debug
{
	[RequireComponent(typeof(TMP_Text))]
	public class FPSCounter : MonoBehaviour
	{
		private const float fpsMeasurePeriod = 0.5f;
		private int m_FpsAccumulator;
		private float m_FpsNextPeriod;
		private int m_CurrentFps;
		private const string display = "{0} FPS";
		private TMP_Text m_Text;

		private void Start()
		{
			m_FpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;
			m_Text = GetComponent<TMP_Text>();
		}

		private void Update()
		{
			// measure average frames per second
			m_FpsAccumulator++;
			if (Time.realtimeSinceStartup > m_FpsNextPeriod)
			{
				m_CurrentFps = (int)(m_FpsAccumulator / fpsMeasurePeriod);
				m_FpsAccumulator = 0;
				m_FpsNextPeriod += fpsMeasurePeriod;
				m_Text.text = string.Format(display, m_CurrentFps);
			}
		}
	}
}
