using System;
using System.Collections;
using System.Collections.Generic;
using DavidUtils.DevTools.Reflection;
using UnityEngine;

namespace DavidUtils.DevTools.Testing
{
	public abstract class TestRunner : MonoBehaviour
	{
		public struct TestInfo
		{
			public string name;
			public Func<bool> successCondition;
			public Action onSuccess;
			public Action onFailure;
			
			public TestInfo(string name, Func<bool> successCondition = null, Action onSuccess = null, Action onFailure = null)
			{
				this.name = name;
				this.successCondition = successCondition;
				this.onSuccess = onSuccess;
				this.onFailure = onFailure;
			}

			public TestInfo(Func<bool> successCondition): this(null, successCondition) { }
		}
        
		[Header("Tests")]
		public bool runOnStart;
		public bool autoRun;
		public float waitSeconds = 1;

		protected Action OnStartTest;
		protected Action OnEndTest;

		// TESTS => [Test Action, Test Condition]
		protected Dictionary<Action, TestInfo> tests = new();

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

		public void AddTest(Action test, TestInfo info) => tests.Add(test, info);
		public void AddTest(Action test, Func<bool> condition = null) => tests.Add(test, new TestInfo(condition));
		public void AddTest(Action test, string name) => tests.Add(test, new TestInfo(name));

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
			foreach ((Action test, TestInfo info) in tests)
			{
				test();
				
				bool success = info.successCondition?.Invoke() ?? true;
				LogTest(info.name, success);
				
				if (success) info.onSuccess?.Invoke();
				else info.onFailure?.Invoke();
				
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
