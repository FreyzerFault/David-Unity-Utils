using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.UI.Fonts;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.DevTools.InspectorUtilities
{
    public static class TableFields
    {
        private static GUIStyle DefaultTextStyle => MyFonts.SmallMonoStyle;

        private static float CalcHeight(this GUIStyle style, int rows = 1) =>
            style.CalcHeight(
                new GUIContent(string.Join("", "\n".ToFilledArray(rows))),
                EditorGUIUtility.currentViewWidth - 50);
        
        public static void Table(IEnumerable<string> list, ref Vector2 scrollPos, string header = null, int maxRowsVisible = 20)
        {
            var tableContent = new GUIContent(string.Join("\n", list));
            Vector2 tableSize = DefaultTextStyle.CalcSize(tableContent);

            if (header != null)
            {
                EditorGUILayout.BeginScrollView(new Vector2(scrollPos.x, 0), false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(header, DefaultTextStyle, GUILayout.Width(tableSize.x));
                EditorGUILayout.LabelField("     ", GUILayout.Width(GUI.skin.verticalScrollbar.fixedWidth));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, true, true,
                GUILayout.MaxHeight(DefaultTextStyle.CalcHeight(maxRowsVisible)));
                
            EditorGUILayout.LabelField(tableContent, DefaultTextStyle, GUILayout.Height(tableSize.y),
                GUILayout.Width(tableSize.x));
                    
            EditorGUILayout.EndScrollView();
        }
        
        public static void ExpandableTable(
            IEnumerable<string> list, ref int numVisible, ref Vector2 scrollPos, string header = null, Action onExpand = null, int maxRowsVisible = 20)
        {
            EditorGUILayout.BeginHorizontal(new GUIStyle { alignment = TextAnchor.MiddleLeft },
                GUILayout.ExpandWidth(false), GUILayout.MinWidth(0));

            int newNumVisible = CounterGUI(numVisible, 10, list.Count());
            if (newNumVisible != numVisible) onExpand?.Invoke();
            numVisible = newNumVisible;
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();
            
            Table(list.Take(numVisible), ref scrollPos, header, maxRowsVisible);
        }

        private static int CounterGUI(int counter, int increment, int max)
        {
            if (GUILayout.Button("-", GUILayout.MaxWidth(20))) counter -= increment;
            if (GUILayout.Button("+", GUILayout.MaxWidth(20))) counter += increment;
            EditorGUILayout.LabelField($"{counter} / {max}",
                GUILayout.ExpandWidth(false), GUILayout.MinWidth(0));

            return Mathf.Clamp(counter, 0, max);
        }
    }
}
