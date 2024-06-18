using System;
using System.Collections;
using System.Collections.Generic;
using DavidUtils.DevTools.Reflection;
using UnityEngine;

namespace DavidUtils.DevTools.Testing
{
	public abstract class TestRunner : MonoBehaviour
	{
		[Header("Tests")]
		public bool runOnStart;
		public bool autoRun;
		public float waitSeconds = 1;

		protected Action OnStartTest;
		protected Action OnEndTest;

		// TESTS => [Test Action, Test Condition]
		protected Dictionary<Action, Func<bool>> tests = new();

		protected Coroutine testsCoroutine;
		protected bool playing = true;
		[ExposedField] public bool IsPlaying => playing;
		[ExposedField] public string PlayingStr => playing ? "Playing" : "Paused";

		protected virtual void Awake()
		{
			if (runOnStart) StartTests();
		}

		private void Update()
		{
			// SPACE => Pause / Resume TESTS
			if (Input.GetKeyDown(KeyCode.Space)) TogglePlaying();
		}

		public void AddTest(Action test, Func<bool> condition = null) => tests.Add(test, condition);

		public void StartTests()
		{
			playing = true;
			if (testsCoroutine != null) EndTests();
			testsCoroutine = StartCoroutine(autoRun ? RunTests_Auto(waitSeconds) : RunTests_Single());
		}

		public void EndTests()
		{
			StopCoroutine(testsCoroutine);
			testsCoroutine = null;
			playing = false;
		}

		public void TogglePlaying() => playing = !playing;
		public void PauseTests() => playing = false;
		public void ResumeTests() => playing = true;

		public IEnumerator RunTests_Single()
		{
			playing = true;
			OnStartTest?.Invoke();
			yield return TestsCoroutine();
			OnEndTest?.Invoke();
			playing = false;
		}

		public IEnumerator RunTests_Auto(float waitSeconds, Action before = null, Action after = null)
		{
			playing = true;
			while (playing)
			{
				(before ?? OnStartTest)?.Invoke();
				yield return new WaitForSeconds(waitSeconds);
				yield return TestsCoroutine();
				yield return new WaitUntil(() => playing);
				(after ?? OnEndTest)?.Invoke();
			}
		}

		private IEnumerator TestsCoroutine()
		{
			foreach ((Action test, Func<bool> condition) in tests)
			{
				test();
				LogTest(test.Method.Name, condition?.Invoke() ?? true);
				yield return new WaitForSeconds(waitSeconds);
				yield return new WaitUntil(() => playing);
			}
		}

		private void LogTest(string testName, bool success, string msg = null)
		{
			if (success)
				Debug.Log($"<color=#00ff00><b>\u2714 Test: {testName}</b> - {msg ?? "Success"}</color>", this);
			else
				Debug.LogError($"<color=#ff0000><b>\u2716 Test: {testName}</b> - {msg ?? "Failed"}</color>", this);
		}
	}
}
