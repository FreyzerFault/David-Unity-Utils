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
        
        private GUIStyle _selectedTestStyle;
        private GUIStyle _iconStyle;
        private GUIStyle _bigIconStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _testLabelStyle;
        
        private TestManager manager => (TestManager) target;

        private void OnEnable()
        {
            playIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/play");
            pauseIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/pause");
            circleIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/circle");
            checkIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/check");
            crossIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/cross");
            restartIcon = Resources.Load<Texture2D>("Textures/Icons/Editor Icons/restart");
            
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
            
            EditorGUILayout.Space();
            
            PlayingUI();
            
            EditorGUILayout.Space();
            
            SettingsUI();         
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            TestListUI();

            RestartUI();
        }

        private void PlayingUI()
        {
            if (playIcon == null || pauseIcon == null)
            {
                Debug.LogError("Play and Pause Icons not found");
                return;
            }
            
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(manager.IsPlaying ? pauseIcon : playIcon, _iconStyle)) 
                    manager.TogglePlay();
                
                GUILayout.Space(10);
                
                GUILayout.Label($"Test {manager.currentTestIndex}", _titleStyle);
                
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        private void SettingsUI()
        {
            EditorGUI.BeginChangeCheck();
            
            manager.runOnStart = EditorGUILayout.ToggleLeft("Run On Start", manager.runOnStart);
            
            manager.waitSecondsBetweenTests = EditorGUILayout.FloatField("Secs Between Tests", manager.waitSecondsBetweenTests, GUILayout.Width(200));
            
            if (EditorGUI.EndChangeCheck()) { }
        }

        private void TestListUI()
        {
            EditorGUILayout.LabelField("Test Runners", EditorStyles.whiteLargeLabel);
            EditorGUILayout.Space();
            GUILayout.BeginVertical();
            {
                // No Test Loaded
                if (manager.testRunners.IsNullOrEmpty() && GUILayout.Button("Load Child Test Runners"))
                    manager.LoadTestRunners();
                
                // Colored blue
                manager.testRunners.ForEach(RunnerUI);
            }
            GUILayout.EndVertical();
        }

        private void RunnerUI(TRunner runner, int index)
        {
            bool isCurrent = index == manager.currentTestIndex;
            bool isPlaying = isCurrent && runner.IsPlaying;
            bool isPaused = isCurrent && !runner.IsPlaying;
            bool isEnded = runner.HasEndedAtLeastOnce;
            
            bool allSuccess = isEnded && runner.successList.All(dict => dict.Values.All(ok => ok));
                    
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(
                    isEnded 
                        ? (allSuccess ? checkIcon : crossIcon) 
                        : isPlaying
                            ? playIcon 
                            : isPaused 
                                ? pauseIcon
                                : circleIcon,
                    
                    EditorStyles.iconButton);
                EditorGUILayout.LabelField(runner.name,
                    index == manager.currentTestIndex ? _selectedTestStyle : _testLabelStyle);
                
                GUILayout.FlexibleSpace();

                manager.iterations[index] = EditorGUILayout.IntField(manager.iterations[index], GUILayout.Width(50));
            }
            GUILayout.EndHorizontal();
        }

        private void RestartUI()
        {
            Rect absoluteRect = new(Screen.width - 40, 10, Screen.width - 10, 40);
            if (GUI.Button(absoluteRect, restartIcon, _bigIconStyle)) 
                manager.RestartTests();
        }
    }
}
