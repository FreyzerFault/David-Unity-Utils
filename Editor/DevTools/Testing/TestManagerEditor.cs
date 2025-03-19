using System.Linq;
using DavidUtils.DevTools.Testing;
using DavidUtils.ExtensionMethods;
using UnityEditor;
using UnityEngine;
using TRunner = DavidUtils.DevTools.Testing.TestRunner;

namespace DavidUtils.Editor.DevTools.Testing
{
    [CustomEditor(typeof(TestManager))]
    public class TestManagerEditor : UnityEditor.Editor
    {
        private Texture2D _playIcon;
        private Texture2D _pauseIcon;
        private Texture2D _circleIcon;
        private Texture2D _checkIcon;
        private Texture2D _crossIcon;
        private Texture2D _restartIcon;
        private Texture2D _arrowRightIcon;
        private Texture2D _arrowLeftIcon;
        
        private GUIStyle _selectedTestStyle;
        private GUIStyle _iconStyle;
        private GUIStyle _bigIconStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _testLabelStyle;
        
        private TestManager Manager => (TestManager) target;

        private void OnEnable()
        {
            // To Update Single TestInfo in IU
            Manager.testRunners.ForEach(t =>
            {
                t.onStartSingleTest += _ => Repaint();
                t.onEndSingleTest += _ => Repaint();
            });
        }

        public override void OnInspectorGUI()
        {
            if (Manager == null) return;
            
            InitializeStyles();
            InitializeIcons();

            {   // ABSOLUTE
                PlayPauseUI();
                RestartUI();
            }
                
            EditorGUILayout.Space();
            
            PlayingUI();
            
            EditorGUILayout.Space();
            
            SettingsUI();         
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            TestListUI();
        }
        

        #region UI

        private void PlayingUI()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{(Manager.IsPlaying ? "RUNNING" : "PAUSED")}", _titleStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void SettingsUI()
        {
            EditorGUI.BeginChangeCheck();
            
            Manager.runOnStart = EditorGUILayout.ToggleLeft("Run On Start", Manager.runOnStart);
            
            Manager.WaitSecondsBetweenTests = EditorGUILayout.FloatField("Secs Between Tests", Manager.WaitSecondsBetweenTests, GUILayout.Width(200));
            
            if (EditorGUI.EndChangeCheck()) { }
        }

        private void TestListUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField("Test Runners", EditorStyles.whiteLargeLabel);
                
                    EditorGUILayout.Space();
                
                    // Colored blue
                    Manager.testRunners.ForEach(RunnerUI);
                }
                GUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();
                
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace(); // To align right
                        EditorGUILayout.LabelField("Iteration", EditorStyles.label, GUILayout.Width(60));
                    }
                    GUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space();
                    
                    // NUM ITERATION FIELD
                    const float numWidth = 30;
                    foreach (TRunner testRunner in Manager.testRunners)
                    {
                        if (!Manager.iterationsByTest.TryGetValue(testRunner, out int iterations))
                            iterations = 1;
                        
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace(); // To align right
                            if (Application.isPlaying)
                            {
                                if (iterations > 1) // Show Current Iteration if there's more than 1
                                    GUILayout.Label($"{testRunner.Iteration + 1}", GUILayout.Width(numWidth));
                            }
                            else 
                                Manager.SetIteration(testRunner,
                                    EditorGUILayout.IntField(iterations, GUILayout.Width(numWidth)));
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            
            
            // No Test Loaded Message and buttons
            if (Manager.testRunners.NotNullOrEmpty()) return;
            // Check Children Test Runners
            TRunner[] testRunners = Manager.GetComponentsInChildren<TRunner>(true);
            EditorGUILayout.LabelField(testRunners.IsNullOrEmpty()
                    ? "No Tests Found\nAdd Test Runners under this Object in the hierarchy"
                    : "No Active Tests.\nYou can activate tests individualy.\nOnly Active Tests are executed",
                EditorStyles.wordWrappedLabel);
                        
            // Button to ACTIVATE children if exists and ALL are INACTIVE
            if (testRunners.NotNullOrEmpty() && GUILayout.Button("Activate ALL Child Tests")) 
                testRunners.ForEach(t =>
                {
                    t.gameObject.SetActive(true);
                    Manager.LoadTestRunners();
                });
        }

        private void RunnerUI(TRunner testRunner, int index)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    // ICON
                    GUILayout.Label(GetIcon(testRunner, index), EditorStyles.iconButton);
                    
                    // TEST Runner NAME
                    EditorGUILayout.LabelField(testRunner.name,
                        index == Manager.currentTestIndex ? _selectedTestStyle : _testLabelStyle);
                }
                GUILayout.EndHorizontal();
            
                // UNIT TEST RUNNING
                if (Manager.currentTestIndex == index) // Show ALL Tests
                {
                    foreach (TRunner.TestInfo info in testRunner.TestsInfo) 
                        UnitTestUI(testRunner, info);
                }
                else if (testRunner.AnyTestFailed) // Show Failed Tests
                {
                    foreach (TRunner.TestInfo info in testRunner.TestsInfo)
                    {
                        if (testRunner.CurrentSuccessDict.TryGetValue(info.name, out bool success) && !success) 
                            UnitTestUI(testRunner, info);
                    }
                }
            }
            
            GUILayout.EndVertical();
        }

        private void UnitTestUI(TRunner test, TRunner.TestInfo info, float indentMargin = 20)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(indentMargin);
                
                // ICON
                var successDict = test.Iteration >= test.successList.Count ? null : test.successList?[test.Iteration];
                if (successDict != null && successDict.TryGetValue(info.name, out bool success)) // ENDED Test
                    GUILayout.Label(success ? _checkIcon : _crossIcon, EditorStyles.iconButton);
                else // NOT STARTED Test
                    GUILayout.Label(test.currentTestInfo == info ? _arrowRightIcon : _circleIcon,
                        EditorStyles.iconButton);
                
                // UNIT TEST NAME
                EditorGUILayout.LabelField($"{info.name}", _testLabelStyle);

            }
            GUILayout.EndHorizontal();
        }

        private void PlayPauseUI()
        {
            if (GUI.Button(GetCornerRect(new Vector2(30,30), 10), Manager.IsPlaying ? _pauseIcon : _playIcon, _bigIconStyle))
            {
                if (Application.isPlaying)
                    Manager.TogglePlay();
                else // Execute in Editor Play Mode
                {
                    Manager.runOnStart = true;
                    EditorApplication.EnterPlaymode();
                }
            }
        }

        private void RestartUI()
        {
            if (GUI.Button(GetCornerRect(new Vector2(30,30), 10, Corner.TopRight), _restartIcon, _bigIconStyle)) 
                Manager.RestartTests();
        }

        #endregion

        
        #region INITIALIZATION

        private void InitializeIcons()
        {
            _playIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/play");
            _pauseIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/pause");
            _circleIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/circle");
            _checkIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/check");
            _crossIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/cross");
            _restartIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/restart");
            _arrowRightIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/arrow right");
            _arrowLeftIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/arrow left");
        }

        private void InitializeStyles()
        {
            _iconStyle = new GUIStyle(EditorStyles.iconButton)
            {
                fixedWidth = 20,
                fixedHeight = 20
            };
            _bigIconStyle = new GUIStyle(_iconStyle)
            {
                fixedWidth = 30,
                fixedHeight = 30
            };
            _titleStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                padding = new RectOffset(0,0,0,0),
                margin = new RectOffset(0,0,0,0)
            };
            
            _testLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0,0,0,5),
                margin = new RectOffset(0,0,0,0)
            };
            _selectedTestStyle = new GUIStyle(_testLabelStyle)
            {
                normal =
                {
                    textColor = Color.cyan
                }
            };
        }

        #endregion

        
        #region UTILS

        private enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }
        private Rect GetCornerRect(Vector2 size, float margin, Corner corner = Corner.TopLeft)
        {
            float width = EditorGUIUtility.currentViewWidth; 
            Vector3 pos = corner switch
            {
                Corner.TopLeft => new Vector3(margin, margin),
                Corner.TopRight => new Vector3(width - size.x - margin, margin),
                Corner.BottomLeft => new Vector3(margin, Screen.height - size.y - margin),
                Corner.BottomRight => new Vector3(width - size.x - margin, Screen.height - size.y - margin),
                _ => new Vector3(0,0)
            };
            return new Rect(pos.x, pos.y, size.x, size.y);
        }

        private Texture2D GetIcon(TRunner test, int index)
        {
            bool isCurrent = index == Manager.currentTestIndex;
            bool isPlaying = isCurrent && test.IsPlaying;
            bool isPaused = isCurrent && !test.IsPlaying;
            bool isEnded = test.HasEndedAtLeastOnce;
            
            bool allSuccess = isEnded && test.successList.Last().Values.All(ok => ok);
                    
            return isEnded
                ? allSuccess
                    ? _checkIcon
                    : _crossIcon
                : isPlaying
                    ? _playIcon
                    : isPaused 
                        ? _pauseIcon
                        : _circleIcon;
        }

        #endregion
    }
}
