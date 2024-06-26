using System;
using System.Collections;
using System.Collections.Generic;
using DavidUtils.DevTools.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

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

			public TestInfo(
				string name, Func<bool> successCondition = null, Action onSuccess = null, Action onFailure = null
			)
			{
				this.name = name;
				this.successCondition = successCondition;
				this.onSuccess = onSuccess;
				this.onFailure = onFailure;
			}

			public TestInfo(Func<bool> successCondition) : this(null, successCondition)
			{
			}
		}

		[Header("Tests")]
		public bool runOnStart;
		public bool autoRun;
		public float waitSeconds = 1;

		protected int iterations;

		protected Action OnStartTest;
		protected Action OnEndTest;

		// TESTS => [Test Action, Test Condition]
		protected Dictionary<Func<IEnumerator>, TestInfo> tests = new();

		protected Coroutine testsCoroutine;
		protected bool playing = true;
		[ExposedField] public bool IsPlaying => playing;
		[ExposedField] public string PlayingStr => playing ? "PLAYING" : "PAUSED";
		[ExposedField] public string ToggleLabel => playing ? "STOP" : "PLAY";

		protected virtual void Awake()
		{
			tests = new Dictionary<Func<IEnumerator>, TestInfo>();
			InitializeTests();
		}

		protected virtual void Start()
		{
			if (runOnStart) StartTests();
		}

		private void Update()
		{
			// SPACE => Pause / Resume TESTS
			if (Input.GetKeyDown(KeyCode.Space)) TogglePlaying();
		}


		protected abstract void InitializeTests();

		public void AddTest(Func<IEnumerator> test, TestInfo info) => tests.Add(test, info);

		public void AddTest(Func<IEnumerator> test, Func<bool> condition = null) =>
			tests.Add(test, new TestInfo(condition));

		public void AddTest(Func<IEnumerator> test, string name) => tests.Add(test, new TestInfo(name));

		public void AddTest(Action test, TestInfo info) => AddTest(ActionToCoroutine(test), info);

		public void AddTest(Action test, Func<bool> condition = null) =>
			AddTest(ActionToCoroutine(test), new TestInfo(condition));

		public void AddTest(Action test, string name) => AddTest(ActionToCoroutine(test), new TestInfo(name));

		private Func<IEnumerator> ActionToCoroutine(Action action) => () =>
		{
			action();
			return null;
		};

		public void StartTests()
		{
			iterations = 0;
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
			iterations++;
			playing = false;
		}

		public IEnumerator RunTests_Auto(float waitSeconds, Action before = null, Action after = null)
		{
			playing = true;
			while (playing)
			{
				(before ?? OnStartTest)?.Invoke();
				yield return TestsCoroutine();
				yield return new WaitForSeconds(waitSeconds);
				yield return new WaitUntil(() => playing);
				(after ?? OnEndTest)?.Invoke();
				iterations++;
			}
		}

		private IEnumerator TestsCoroutine()
		{
			foreach ((Func<IEnumerator> test, TestInfo info) in tests)
			{
				yield return test();

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
			string color = success ? "#00ff00" : "#ff0000";
			string symbol = success ? "\u2714" : "\u2716";
			string numTests = autoRun ? $"#{iterations}" : "";
			msg ??= success ? "Success" : "Failed";
			Action<string, Object> logFunction = success ? Debug.Log : Debug.LogError;
			logFunction($"<color={color}><b>{symbol} {numTests} Test: {testName}</b> - {msg}</color>", this);
		}
	}
}
