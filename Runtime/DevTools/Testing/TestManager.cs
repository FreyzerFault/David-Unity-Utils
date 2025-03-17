using System;
using System.Collections;
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
    public class TestManager : Singleton<TestManager>
    {
        public bool runOnStart = true;
        public float waitSecondsBetweenTests = 1;
        
        public TestRunner[] testRunners;
        public int currentTestIndex = 0;

        public int[] iterations;

        public Action onEnd; 
        
        public TestRunner CurrentTestRunner => testRunners[currentTestIndex];
        
        private bool playing = false;
        public bool IsPlaying => playing;

        protected override void Awake()
        {
            base.Awake();
            LoadTestRunners();
        }

        public void LoadTestRunners()
        {
            // Get all TestRunners in children
            testRunners = GetComponentsInChildren<TestRunner>(true);
            
            // Don't run ANY test on start, the Manager already start them
            testRunners.ForEach(r =>
            {
                r.runOnStart = false;
                r.gameObject.SetActive(false);
            });
            
            // Default iteration to 1
            if (iterations.IsNullOrEmpty())
                iterations = 1.ToFilledArray(testRunners.Length).ToArray();
        }

        private void Start()
        {
            if (!runOnStart) return;
            SelectTest(0);
            StartCoroutine(AutoTestingCoroutine());
        }

        
        private IEnumerator AutoTestingCoroutine()
        {
            playing = true;
            while (playing)
            {
                yield return CurrentTestRunner.RunTests_Repeated(iterations[currentTestIndex]);
                yield return new WaitForSeconds(waitSecondsBetweenTests);
                yield return new WaitUntil(() => playing);
                
                if (currentTestIndex == testRunners.Length - 1)
                    playing = false;

                SelectTest(currentTestIndex + 1);
            }
            onEnd?.Invoke();
        }

        public void SelectTest(int i)
        {
            CurrentTestRunner.PauseTests();
            CurrentTestRunner.gameObject.SetActive(false);
            
            currentTestIndex = i;
            
            if (currentTestIndex >= testRunners.Length) return;
            CurrentTestRunner.gameObject.SetActive(true);
        }

        public void StartTest(int i)
        {
            SelectTest(i);
            StartTest();
        }
        
        public void StartTest()
        {
            CurrentTestRunner.StartTests();
            playing = true;
        }

        public void EndTests()
        {
            CurrentTestRunner.EndTests();
            playing = false;
        }

        public void PauseTest()
        {
            CurrentTestRunner.PauseTests();
            playing = false;
        }
        
        public void TogglePlay()
        {
            if (playing)
                PauseTest();
            else
                ResumeTest();
        }

        public void ResumeTest()
        {
            CurrentTestRunner.ResumeTests();
            playing = true;
        }

        public void RestartTests()
        {
            if (currentTestIndex < 0 || currentTestIndex >= testRunners.Length) return;
            EndTests();
            SelectTest(0);
            StartCoroutine(AutoTestingCoroutine());
        }
    }
}
