using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace DavidUtils.DevTools.Testing
{
    /// <summary>
    ///     Test Runner Wrapper to Manage a group of TestRunners
    ///     Works like a Single TestRunner
    ///     You can Select a single TestRunner and run it
    ///     or Run ALL of them automaticaly
    /// </summary>
    [ExecuteAlways]
    public class TestManager : Singleton<TestManager>
    {
        public bool runOnStart = true;
        
        [SerializeField] private float waitSecondsBetweenTests = 1;
        public float WaitSecondsBetweenTests
        {
            get => waitSecondsBetweenTests;
            set
            {
                waitSecondsBetweenTests = value;
                foreach (TestRunner testRunner in testRunners) 
                    testRunner.waitSeconds = waitSecondsBetweenTests;
            }
        }

        public TestRunner[] testRunners;
        public int currentTestIndex;

        public Dictionary<TestRunner, int> iterationsByTest = new();
        [SerializeField] private List<int> iterationsList = new();

        public int[] Iterations
        {
            get => iterationsList.ToArray();
            set
            {
                iterationsList = value.ToList();
                for (var i = 0; i < testRunners.Length; i++)
                    if (!iterationsByTest.TryAdd(testRunners[i], iterationsList[i]))
                        iterationsByTest[testRunners[i]] = iterationsList[i];
            }
        }

        public Action onEnd; 
        
        public TestRunner CurrentTestRunner => 
            currentTestIndex < 0 || currentTestIndex >= testRunners.Length 
                ? null
                : testRunners[currentTestIndex];
        
        private bool _playing;
        public bool IsPlaying => _playing;

        protected override void Awake()
        {
            base.Awake();
            LoadTestRunners();
        }

        public void LoadTestRunners()
        {
            // Get all TestRunners in children
            testRunners = GetComponentsInChildren<TestRunner>(false);
            
            // Don't run ANY test on start, the Manager already start them
            testRunners.ForEach(r => r.runOnStart = false);
            
            // Default iteration to 1 (TryAdd to avoid overwriting)
            for (var i = 0; i < iterationsList.Count; i++)
                iterationsByTest.TryAdd(testRunners[i], iterationsList[i]);
        }

        private void Start()
        {
            // Only in Play Mode
            if (!Application.isPlaying) return;

            if (testRunners.IsNullOrEmpty()) return;
            
            // Deactivate before starting tests
            testRunners.ForEach(t => t.gameObject.SetActive(false));

            if (!runOnStart) return;
            
            SelectTest(0);
            StartCoroutine(AutoTestingCoroutine());
        }

        // Actualiza los Test Runners para usar solo los ACTIVOS
        // Así si desactivas uno desaparece y cuando activas uno se añade
        // Cuando cambian los TestRunners activos ejecuta LoadTestRunners
        private void Update()
        {
            if (Application.isPlaying) return;
            TestRunner[] activeTestRunners = GetComponentsInChildren<TestRunner>(false);
            if (!activeTestRunners.ContentsMatch(testRunners))
                LoadTestRunners();
        }
        
        public void SetIteration(TestRunner test, int iterations)
        {
            // Save in the Dictionary<TestRunner, int>
            if (!iterationsByTest.TryAdd(test, iterations))
                iterationsByTest[test] = iterations;
            
            // Save in the List<int> keeping the test Order
            iterationsList = iterationsByTest.Values.ToList();
        }


        private IEnumerator AutoTestingCoroutine()
        {
            _playing = true;
            while (_playing)
            {
                if (CurrentTestRunner == null)
                    SelectTest(0);
                
                if (iterationsByTest.TryGetValue(CurrentTestRunner, out int iters))
                    yield return CurrentTestRunner.RunTests_Repeated(iters);
                else
                    yield return CurrentTestRunner.RunTests_Single();
                    
                yield return new WaitForSeconds(waitSecondsBetweenTests);
                yield return new WaitUntil(() => _playing);
                
                if (currentTestIndex == testRunners.Length - 1)
                    _playing = false;

                SelectTest(currentTestIndex + 1);
            }
            onEnd?.Invoke();
        }

        public void SelectTest(int i)
        {
            CurrentTestRunner?.PauseTests();
            CurrentTestRunner?.gameObject.SetActive(false);
            
            currentTestIndex = i;
            CurrentTestRunner?.gameObject.SetActive(true);
        }

        public void StartTest(int i)
        {
            SelectTest(i);
            StartTest();
        }
        
        public void StartTest()
        {
            CurrentTestRunner?.StartTests();
            _playing = true;
        }

        public void EndTests()
        {
            CurrentTestRunner?.EndTests();
            _playing = false;
        }

        public void PauseTest()
        {
            CurrentTestRunner?.PauseTests();
            _playing = false;
        }
        
        public void TogglePlay()
        {
            if (_playing)
                PauseTest();
            else
                ResumeTest();
        }

        public void ResumeTest()
        {
            if (testRunners.All(t => t.ended))
                RestartTests();
            else
            {
                CurrentTestRunner?.ResumeTests();
                _playing = true;
            }
        }

        public void RestartTests()
        {
            foreach (TestRunner testRunner in testRunners) 
                testRunner.ResetTests();
            
            SelectTest(0);
            StartCoroutine(AutoTestingCoroutine());
        }
    }
}
