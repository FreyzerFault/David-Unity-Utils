using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.DevTools.Table;
using DavidUtils.ExtensionMethods;
using DavidUtils.UI.Fonts;
using UnityEditor;
using UnityEngine;

namespace DavidUtils.Editor.DevTools.Table
{
    public static class TableFields
    {
        private static GUIStyle DefaultTextStyle => MyFonts.SmallMonoStyle;
        
        
        private static float CalcHeight(this GUIStyle style, int rows = 1) =>
            style.CalcHeight(
                new GUIContent(string.Join("", "\n".ToFilledArray(rows))),
                EditorGUIUtility.currentViewWidth - 50);

        #region TABLES

        
        /// <summary>
        ///     Inspector Scrollable Table
        ///     Header stays while scrolling
        /// </summary>
        public static void Table(IEnumerable<string> lines, ref Vector2 scrollPos, string header = null, int maxRowsVisible = 20)
        {
            var tableContent = new GUIContent(string.Join("\n", lines));
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
        
        // Table[line][value]
        // Add the Value Lenghts to calculate the column width
        public static void Table(IEnumerable<IEnumerable<string>> values, ref Vector2 scrollPos, int[] valueLengths, string header = null, int maxRowsVisible = 20) => 
            Table(values.Select(v => v.ToTableLine(valueLengths)), ref scrollPos, header, maxRowsVisible);

        /// <summary>
        ///     Scrollable & Expandable Table
        ///     Table don't show ALL values for performance reasons
        ///     Use the counter to expand or shrink the table by the increment
        /// </summary>
        public static void ExpandableTable(
            IEnumerable<string> lines, ref int numVisible, ref Vector2 scrollPos,
            string header = null, Action onExpand = null, int maxRowsVisible = 20, int increment = 10)
        {
            EditorGUILayout.BeginHorizontal(new GUIStyle { alignment = TextAnchor.MiddleLeft },
                GUILayout.ExpandWidth(false), GUILayout.MinWidth(0));

            int newNumVisible = CounterField(numVisible, increment, lines.Count());
            if (newNumVisible != numVisible) onExpand?.Invoke();
            numVisible = newNumVisible;
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();
            
            Table(lines.Take(numVisible), ref scrollPos, header, maxRowsVisible);
        }

        #endregion


        #region AUX COMPONENTS

        /// <summary>
        ///     A simple counter Field with ADD and SUBSTRACT buttons
        ///     [-] [+] x / N
        /// </summary>
        public static int CounterField(int counter, int increment, int max)
        {
            if (GUILayout.Button("-", GUILayout.MaxWidth(20))) counter -= increment;
            if (GUILayout.Button("+", GUILayout.MaxWidth(20))) counter += increment;
            EditorGUILayout.LabelField($"{counter} / {max}",
                GUILayout.ExpandWidth(false), GUILayout.MinWidth(0));

            return Mathf.Clamp(counter, 0, max);
        }

        #endregion
    }
}
