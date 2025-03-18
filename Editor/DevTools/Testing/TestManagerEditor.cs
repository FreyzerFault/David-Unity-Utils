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
        private Texture2D playIcon;
        private Texture2D pauseIcon;
        private Texture2D circleIcon;
        private Texture2D checkIcon;
        private Texture2D crossIcon;
        private Texture2D restartIcon;
        private Texture2D arrowRightIcon;
        private Texture2D arrowLeftIcon;
        
        private GUIStyle _selectedTestStyle;
        private GUIStyle _iconStyle;
        private GUIStyle _bigIconStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _testLabelStyle;
        
        private TestManager manager => (TestManager) target;

        private void OnEnable()
        {
            // To Update Single TestInfo in IU
            manager.testRunners.ForEach(t =>
            {
                t.onStartSingleTest += _ => Repaint();
                t.onEndSingleTest += _ => Repaint();
            });
            
            playIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/play");
            pauseIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/pause");
            circleIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/circle");
            checkIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/check");
            crossIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/cross");
            restartIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/restart");
            arrowRightIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/arrow right");
            arrowLeftIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/arrow left");

            if (EditorStyles.iconButton == null)
                return;
            
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
        
        

        public override void OnInspectorGUI()
        {
            if (manager == null) return;

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

        private void PlayingUI()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{(manager.IsPlaying ? "RUNNING" : "PAUSED")}", _titleStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void SettingsUI()
        {
            EditorGUI.BeginChangeCheck();
            
            manager.runOnStart = EditorGUILayout.ToggleLeft("Run On Start", manager.runOnStart);
            
            manager.WaitSecondsBetweenTests = EditorGUILayout.FloatField("Secs Between Tests", manager.WaitSecondsBetweenTests, GUILayout.Width(200));
            
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
                
                    // No Test Loaded
                    if (manager.testRunners.IsNullOrEmpty() && GUILayout.Button("Load Child Test Runners"))
                        manager.LoadTestRunners();
                    
                    // Colored blue
                    manager.testRunners.ForEach(RunnerUI);
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
                    manager.testRunners.ForEach((test, index) =>
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace(); // To align right
                            if (Application.isPlaying)
                            {
                                if (manager.iterations[index] > 1) 
                                    GUILayout.Label($"{test.Iteration + 1}", GUILayout.Width(numWidth));
                            }
                            else // INPUT Field while not Playing
                                manager.iterations[index] =
                                    EditorGUILayout.IntField(manager.iterations[index], GUILayout.Width(numWidth));
                        }
                        GUILayout.EndHorizontal();
                    });
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
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
                        index == manager.currentTestIndex ? _selectedTestStyle : _testLabelStyle);
                }
                GUILayout.EndHorizontal();
            
                // UNIT TEST RUNNING
                if (manager.currentTestIndex == index) // Show ALL Tests
                {
                    foreach (TRunner.TestInfo info in testRunner.TestsInfo) 
                        UnitTestUI(testRunner, info);
                }
                else if (testRunner.AnyTestFailed) // Show Failed Tests
                {
                    foreach (TRunner.TestInfo info in testRunner.TestsInfo)
                    {
                        if (!testRunner.CurrentSuccesDict[info.name]) 
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
                    GUILayout.Label(success ? checkIcon : crossIcon, EditorStyles.iconButton);
                else // NOT STARTED Test
                    GUILayout.Label(test.currentTestInfo == info ? arrowRightIcon : circleIcon,
                        EditorStyles.iconButton);
                
                // UNIT TEST NAME
                EditorGUILayout.LabelField($"{info.name}", _testLabelStyle);

            }
            GUILayout.EndHorizontal();
        }

        private void PlayPauseUI()
        {
            if (GUI.Button(GetCornerRect(new Vector2(30,30), 10), manager.IsPlaying ? pauseIcon : playIcon, _bigIconStyle)) 
                manager.TogglePlay();
        }

        private void RestartUI()
        {
            Rect absoluteRect = new(Screen.width - 40, 10, Screen.width - 10, 40);
            if (GUI.Button(absoluteRect, restartIcon, _bigIconStyle)) 
                manager.RestartTests();
        }

        #region UTILS

        private enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }
        private Rect GetCornerRect(Vector2 size, float margin, Corner corner = Corner.TopLeft)
        {
            Vector3 pos = corner switch
            {
                Corner.TopLeft => new Vector3(margin, margin),
                Corner.TopRight => new Vector3(Screen.width - size.x - margin, margin),
                Corner.BottomLeft => new Vector3(margin, Screen.height - size.y - margin),
                Corner.BottomRight => new Vector3(Screen.width - size.x - margin, Screen.height - size.y - margin),
                _ => new(0,0)
            };
            return new Rect(pos.x, pos.y, size.x, size.y);
        }

        private Texture2D GetIcon(TRunner test, int index)
        {
            bool isCurrent = index == manager.currentTestIndex;
            bool isPlaying = isCurrent && test.IsPlaying;
            bool isPaused = isCurrent && !test.IsPlaying;
            bool isEnded = test.HasEndedAtLeastOnce;
            
            bool allSuccess = isEnded && test.successList[index].Values.All(ok => ok);
                    
            return isEnded
                ? allSuccess
                    ? checkIcon
                    : crossIcon
                : isPlaying
                    ? playIcon
                    : isPaused 
                        ? pauseIcon
                        : circleIcon;
        }

        #endregion

        
    }
}
