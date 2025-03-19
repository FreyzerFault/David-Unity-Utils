using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.Reflection;
using DavidUtils.ExtensionMethods;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils.DevTools.Testing
{
	public abstract class TestRunner : MonoBehaviour, IEquatable<TestRunner>
	{
		public struct TestInfo : IEquatable<TestInfo>
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

			public override bool Equals(object obj) => 
				obj != null && (base.Equals(obj) || name.Equals(((TestInfo)obj).name));

			public override string ToString() => name;

			public static bool operator ==(TestInfo a, TestInfo b) => a.name == b.name;
			public static bool operator !=(TestInfo a, TestInfo b) => !(a == b);
			
			public bool Equals(TestInfo other) => name == other.name;
			public override int GetHashCode() => name != null ? name.GetHashCode() : 0;
		}

		[Header("Tests")]
		public bool runOnStart = true;
		public bool autoRun = true;
		public bool logTestInfo = true;
		public bool ended = false;
		
		protected int iteration;
		public int Iteration => iteration;
		
		public float waitSeconds = 1;

		public float WaitMilliseconds
		{
			get => waitSeconds * 1000;
			set => waitSeconds = value / 1000;
		}
		[ExposedField]
		public string WaitMillisecondsStr
		{
			get => WaitMilliseconds.ToString("F0") + " ms";
			set => WaitMilliseconds = float.Parse(value.Replace(" ms", ""));
		}


		
		#region TIMERS

		// "Test A": [23, 43, 46, 32, ...]
		private readonly Dictionary<string, List<float>> _testTimes = new();
		
		public float LastTime(string testName) => _testTimes.TryGetValue(testName, out var times) ? times.Last() : 0;
		public float AverageTime(string testName) => _testTimes.TryGetValue(testName, out var times) ? times.Average() : 0;
		public float LastTotalTime => _testTimes.Values.Select(times => times.Last()).Sum();
		public float AverageTotalTime => _testTimes.Values.Select(times => times.Average()).Sum();
		
		public string LastTimeStr(string testName) => $"{LastTime(testName):F0} ms"; 
		public string AverageTimeStr(string testName) => AverageTime(testName).ToString("F0") + " ms"; 
		public string LastTotalTimeStr(string testName) => LastTotalTime.ToString("F0") + " ms"; 
		public string AverageTotalTimeStr(string testName) => AverageTotalTime.ToString("F0") + " ms"; 

		#endregion
		

		protected Action onStartAllTests;
		protected Action onEndAllTests;
		public Action<TestInfo> onStartSingleTest;
		public Action<TestInfo> onEndSingleTest;

		// TESTS => [Test Action, Test Condition]
		protected Dictionary<Func<IEnumerator>, TestInfo> tests = new();
		public TestInfo[] TestsInfo => tests.Values.ToArray();
		public TestInfo currentTestInfo;

		// RESULTADOS => [Iteration1: {"TestA", True, "TestB", False}, Iteration2: {...}]
		public List<Dictionary<string, bool>> successList = new();
		public Dictionary<string, bool> CurrentSuccessDict => iteration < successList.Count ? successList[iteration] : null;
		public bool AnyTestFailed => CurrentSuccessDict != null && CurrentSuccessDict.Values.Any(b => !b);
		
		protected Coroutine testsCoroutine;
		protected bool playing = false;
		[ExposedField] public bool IsPlaying => playing;
		[ExposedField] public bool HasEndedAtLeastOnce => !playing && successList.NotNullOrEmpty() && successList[0].Count == tests.Count;
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
			iteration = 0;
			playing = true;
			if (testsCoroutine != null) EndTests();
			testsCoroutine = StartCoroutine(autoRun ? RunTests_Auto(onStartAllTests, onEndAllTests) : RunTests_Single());
		}

		public void EndTests()
		{
			if (testsCoroutine != null)
				StopCoroutine(testsCoroutine);
			else StopAllCoroutines();
			
			testsCoroutine = null;
			playing = false;
		}
		
		public void ResetTests()
		{
			EndTests();
			iteration = 0;
			_testTimes.Clear();
			successList.ForEach(dict => dict.Clear());
			successList.Clear();
		}

		public void TogglePlaying() => playing = !playing;
		public void PauseTests() => playing = false;
		public void ResumeTests() => playing = true;
		public void TogglePlay() => playing = !playing;

		public IEnumerator RunTests_Single()
		{
			playing = true;
			onStartAllTests?.Invoke();
			yield return TestsCoroutine();
			onEndAllTests?.Invoke();
			iteration++;
			playing = false;
			ended = true;
		}
		
		public IEnumerator RunTests_Repeated(int numIterations, Action before = null, Action after = null)
		{
			playing = true;
			for (iteration = 0; iteration < numIterations; iteration++)
			{
				(before ?? onStartAllTests)?.Invoke();
				yield return TestsCoroutine();
				(after ?? onEndAllTests)?.Invoke();

				if (iteration < numIterations - 1) // Si fuera la ultima que acaba de acabar no hace falta pausarlo
					yield return new WaitUntil(() => playing);
			}

			ended = true;
			iteration -= 1;
		}

		public IEnumerator RunTests_Auto(Action before = null, Action after = null)
		{
			playing = true;
			while (playing)
			{
				(before ?? onStartAllTests)?.Invoke();
				yield return TestsCoroutine();
				(after ?? onEndAllTests)?.Invoke();
				iteration++;
			}

			ended = true;
			iteration -= 1;
		}

		private IEnumerator TestsCoroutine()
		{
			successList.Add(new Dictionary<string, bool>());
			foreach ((Func<IEnumerator> test, TestInfo info) in tests)
			{
				currentTestInfo = info;
				onStartSingleTest?.Invoke(info);
				
				float iniTime = Time.realtimeSinceStartup;
				yield return test();
				float endTime = Time.realtimeSinceStartup;
				float time = (endTime - iniTime) * 1000;

				if (_testTimes.TryGetValue(info.name, out _))
					_testTimes[info.name].Add(time);
				else
					_testTimes.Add(info.name, new List<float> {time});

				bool success = info.successCondition?.Invoke() ?? true;
				
				if (logTestInfo) LogTest(info.name, success, time);

				if (success) info.onSuccess?.Invoke();
				else info.onFailure?.Invoke();

				// Save Result
				if (successList[iteration].TryAdd(info.name, success))
					successList[iteration][info.name] = success;
				
				onEndSingleTest?.Invoke(info);
				
				yield return new WaitForSeconds(waitSeconds);
				yield return new WaitUntil(() => playing);
			}
		}

		private const string red = "#ff3633";
		private const string cyan = "#54faff";
		private const string green = "#47ff3a";

		private void LogTest(string testName, bool success, float time, string msg = null)
		{
			string color = success ? green : red;
			string symbol = success ? "\u2714" : "\u2716";
			string numTests = autoRun ? $"#{iteration}" : "";
			msg ??= success ? "Success" : "Failed";
			Action<string, Object> logFunction = success ? Debug.Log : Debug.LogError;
			logFunction($"<color={color}><b>{symbol} {numTests} ({time:F0} ms) Test: {testName}</b> - {msg}</color>", this);
		}

		public override string ToString()
		{
			string color = IsPlaying ? cyan : HasEndedAtLeastOnce ? green : red;
			string state = IsPlaying ? "Running" : HasEndedAtLeastOnce ? "Ended" : "Paused";
			string currentTest = IsPlaying ? currentTestInfo.ToString() : "";
			string iterations = this.iteration > 0 ? $"[{this.iteration} iterations run]" : "";
			return $"{name}: <color={color}>{state}</color> {currentTest} {iterations}";
		}

		public bool Equals(TestRunner other) => other != null && gameObject.name.Equals(other.gameObject.name);

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((TestRunner)obj);
		}

		public override int GetHashCode() => gameObject.name.GetHashCode();
	}
}
