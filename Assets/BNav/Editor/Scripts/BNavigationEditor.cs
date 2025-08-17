using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BTools.BNav.Editor
{
    /// <summary>
    /// Custom editor for BNavigation component
    /// </summary>
    [CustomEditor(typeof(BNavigation))]
    [CanEditMultipleObjects]
    public class BNavigationEditor : UnityEditor.Editor
    {
        private static readonly GUIContent ContentNone = GUIContent.none;
        private static readonly GUIContent ContentBelongGroup = new GUIContent("Belong Group", "The navigation group this component belongs to");
        private static readonly GUIContent ContentPriority = new GUIContent("Priority", "Priority for navigation selection (higher values have priority when distances are equal)");
        private static readonly GUIContent ContentUp = new GUIContent("Up", "Enable upward navigation");
        private static readonly GUIContent ContentDown = new GUIContent("Down", "Enable downward navigation");
        private static readonly GUIContent ContentLeft = new GUIContent("Left", "Enable leftward navigation");
        private static readonly GUIContent ContentRight = new GUIContent("Right", "Enable rightward navigation");
        private static readonly GUIContent ContentDirectionMode = new GUIContent("Mode", "Global: screen coordinates \nLocal: object's transform");
        private static readonly GUIContent ContentIgnoreRange = new GUIContent("Ignore Range", "Define ranges from object pivot where Selectables will be ignored.");
        private static readonly GUIContent ContentIgnoreRangeMode = new GUIContent("Mode", "Mode for ignore range calculation:\nDisabled: Don't use ignore range\nAutoSyncToSize: Auto sync with RectTransform size\nManual: Use manually configured values");
        private static readonly GUIContent ContentSearchRange = new GUIContent("Search Range", "Define search cone angle for each direction (0-1, where 0.5 ≈ 60°)");
        private static readonly GUIContent ContentTop = new GUIContent("Top");
        private static readonly GUIContent ContentBottom = new GUIContent("Bottom");
        private static readonly GUIContent ContentDebugFollowNavigation = new GUIContent("Follow Navigation", "Enable to follow the navigation target in the scene view during play mode");

        private static readonly GUIContent ContentFallbackNavigations = new GUIContent("Fallback Navigations",
            "Backup navigation targets used when no direct target found in each direction. The closest target in the list will be selected." +
            "\n\nThis is especially useful for implementing loop navigation, e.g., pressing Down on the last item jumps to the first item.");

        private SerializedProperty belongGroupProp;
        private SerializedProperty priorityProp;
        private SerializedProperty enableUpProp;
        private SerializedProperty enableDownProp;
        private SerializedProperty enableLeftProp;
        private SerializedProperty enableRightProp;
        private SerializedProperty directionModeProp;
        private SerializedProperty ignoreTopProp;
        private SerializedProperty ignoreBottomProp;
        private SerializedProperty ignoreLeftProp;
        private SerializedProperty ignoreRightProp;
        private SerializedProperty ignoreRangeModeProp;
        private SerializedProperty searchRangeUpProp;
        private SerializedProperty searchRangeDownProp;
        private SerializedProperty searchRangeLeftProp;
        private SerializedProperty searchRangeRightProp;
        private SerializedProperty fallbackNavigationsUpProp;
        private SerializedProperty fallbackNavigationsDownProp;
        private SerializedProperty fallbackNavigationsLeftProp;
        private SerializedProperty fallbackNavigationsRightProp;

        private BNavigation targetBNav;
        private List<string> availableGroups = new List<string>();
        private string[] groupOptions;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw title at the top
            DrawTitle();

            var topGap = 10f;
            var bottomGap = 0f;

            // Global Settings section
            DrawSeparateLine("Global Settings", 0f, bottomGap);
            DrawGlobalSettingsSection();

            // Group Configuration section
            DrawSeparateLine("Group Configuration", topGap, bottomGap);
            DrawGroupConfigurationSection();

            // Direction Mode section
            DrawSeparateLine("Direction Mode", topGap, bottomGap);
            DrawDirectionModeSection();

            // Navigation Directions section
            DrawSeparateLine("Navigation Directions", topGap, bottomGap);
            DrawNavigationDirectionsSection();

            // Ignore Range section
            DrawSeparateLine(ContentIgnoreRange, topGap, bottomGap);
            DrawIgnoreRangeSection();

            // Search Range section
            DrawSeparateLine(ContentSearchRange, topGap, bottomGap);
            DrawSearchRangeSection();

            // Fallback Navigations section
            DrawSeparateLine(ContentFallbackNavigations, topGap, bottomGap);
            DrawFallbackNavigationsSection();

            // Debug options
            DrawSeparateLine("Debug Tools", topGap * 2f, bottomGap);
            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSeparateLine(string label = null, float topGap = 6f, float bottomGap = 6f)
        {
            EditorGUILayout.Space(topGap);

            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            }

            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1f));

            EditorGUILayout.Space(bottomGap);
        }

        private void DrawSeparateLine(GUIContent content = null, float topGap = 6f, float bottomGap = 6f)
        {
            EditorGUILayout.Space(topGap);

            if (content != null)
            {
                EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
            }

            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1f));

            EditorGUILayout.Space(bottomGap);
        }

        private void OnEnable()
        {
            targetBNav = (BNavigation)target;

            // Find serialized properties
            belongGroupProp = serializedObject.FindProperty("belongGroup");
            priorityProp = serializedObject.FindProperty("priority");
            enableUpProp = serializedObject.FindProperty("enableUp");
            enableDownProp = serializedObject.FindProperty("enableDown");
            enableLeftProp = serializedObject.FindProperty("enableLeft");
            enableRightProp = serializedObject.FindProperty("enableRight");
            directionModeProp = serializedObject.FindProperty("directionMode");
            ignoreRangeModeProp = serializedObject.FindProperty("ignoreRangeMode");
            ignoreTopProp = serializedObject.FindProperty("ignoreTop");
            ignoreBottomProp = serializedObject.FindProperty("ignoreBottom");
            ignoreLeftProp = serializedObject.FindProperty("ignoreLeft");
            ignoreRightProp = serializedObject.FindProperty("ignoreRight");
            searchRangeUpProp = serializedObject.FindProperty("searchRangeUp");
            searchRangeDownProp = serializedObject.FindProperty("searchRangeDown");
            searchRangeLeftProp = serializedObject.FindProperty("searchRangeLeft");
            searchRangeRightProp = serializedObject.FindProperty("searchRangeRight");
            fallbackNavigationsUpProp = serializedObject.FindProperty("fallbackNavigationsUp");
            fallbackNavigationsDownProp = serializedObject.FindProperty("fallbackNavigationsDown");
            fallbackNavigationsLeftProp = serializedObject.FindProperty("fallbackNavigationsLeft");
            fallbackNavigationsRightProp = serializedObject.FindProperty("fallbackNavigationsRight");

            RefreshAvailableGroups();
        }

        private void OnSceneGUI()
        {
            if (targetBNav == null)
            {
                return;
            }

            DrawIgnoreRangeVisualization();
            DrawSearchRangeVisualization();
            DrawNavigationConnections();
        }

        #region Draw Inspector Sections

        private void DrawTitle()
        {
            // Draw background with semi-transparent dark green color
            var headerRect = EditorGUILayout.GetControlRect(false, 30);
            var backgroundColor = new Color(0.2f, 0.4f, 0.2f, 0.7f); // Semi-transparent dark green
            EditorGUI.DrawRect(headerRect, backgroundColor);

            // Draw centered white text
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.BoldAndItalic,
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            EditorGUI.LabelField(headerRect, "BNavigation", headerStyle);

            // Add some space after header
            EditorGUILayout.Space(5);
        }

        private void DrawDebugSection()
        {
            // Only show debug options if global settings are available
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            BNavigation.debugFollowNavigation = EditorGUILayout.Toggle(ContentDebugFollowNavigation, BNavigation.debugFollowNavigation);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawGlobalSettingsSection()
        {
            var globalSettings = BNavSettingsLoader.GlobalSettings;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (globalSettings != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(globalSettings, typeof(BNavGlobalSettings), false);
                    EditorGUI.EndDisabledGroup();

                    if (GUILayout.Button("Edit", GUILayout.Width(40)))
                    {
#if UNITY_2021_1_OR_NEWER
                        EditorUtility.OpenPropertyEditor(globalSettings);
#else
                        Selection.activeObject = globalSettings;
#endif
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("None (using defaults)", EditorStyles.helpBox);
                }

                if (GUILayout.Button("Project Settings", GUILayout.Width(105)))
                {
                    SettingsService.OpenProjectSettings("Project/BNav Settings");
                }
            }
        }

        private void DrawGroupConfigurationSection()
        {
            // Group Type dropdown
            var currentIndex = System.Array.IndexOf(groupOptions, belongGroupProp.stringValue);
            var isValidGroup = currentIndex >= 0 && currentIndex < groupOptions.Length;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(ContentBelongGroup, GUILayout.Width(120));

                if (!isValidGroup)
                {
                    currentIndex = 0;
                }

                EditorGUI.BeginChangeCheck();
                var newIndex = EditorGUILayout.Popup(currentIndex, groupOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(targets, "Change Group Type");

                    // Index 0 is <None>
                    var newGroupName = (newIndex == 0 || newIndex >= groupOptions.Length) ? "" : groupOptions[newIndex];

                    foreach (var obj in targets)
                    {
                        var nav = obj as BNavigation;
                        if (nav != null)
                        {
                            nav.BelongGroup = newGroupName;
                        }
                    }

                    belongGroupProp.stringValue = newGroupName;
                }

                if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                {
                    RefreshAvailableGroups();
                }
            }

            if (!isValidGroup)
            {
                EditorGUILayout.HelpBox(
                    $"Group '{belongGroupProp.stringValue}' not found in Global Settings. It will cause issues with navigation.",
                    MessageType.Warning);
            }

            // Priority
            using (new EditorGUILayout.HorizontalScope())
            {
                var oriLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 120;

                EditorGUI.BeginChangeCheck();

                var newPriority = EditorGUILayout.IntField(ContentPriority, priorityProp.intValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Priority");
                    priorityProp.intValue = newPriority;
                }

                EditorGUIUtility.labelWidth = oriLabelWidth;
            }
        }

        private void DrawNavigationDirectionsSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(enableUpProp, ContentNone, GUILayout.Width(15));
                EditorGUILayout.LabelField(ContentUp, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                EditorGUILayout.PropertyField(enableDownProp, ContentNone, GUILayout.Width(15));
                EditorGUILayout.LabelField(ContentDown, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(enableLeftProp, ContentNone, GUILayout.Width(15));
                EditorGUILayout.LabelField(ContentLeft, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                EditorGUILayout.PropertyField(enableRightProp, ContentNone, GUILayout.Width(15));
                EditorGUILayout.LabelField(ContentRight, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawDirectionModeSection()
        {
            var oriLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 120;

            EditorGUILayout.PropertyField(directionModeProp, ContentDirectionMode);

            EditorGUIUtility.labelWidth = oriLabelWidth;
        }

        private void DrawIgnoreRangeSection()
        {
            // Mode dropdown
            var oriLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 120;
            EditorGUILayout.PropertyField(ignoreRangeModeProp, ContentIgnoreRangeMode);
            EditorGUIUtility.labelWidth = oriLabelWidth;

            // Only show manual controls if mode is Manual
            var ignoreRangeMode = (IgnoreRangeMode)ignoreRangeModeProp.enumValueIndex;
            EditorGUI.BeginDisabledGroup(ignoreRangeMode != IgnoreRangeMode.Manual);

            oriLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 50;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var newIgnoreTop = EditorGUILayout.FloatField(ContentTop, ignoreTopProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Ignore Top");
                    ignoreTopProp.floatValue = Mathf.Max(0f, newIgnoreTop);
                }

                GUILayout.Space(20);

                EditorGUI.BeginChangeCheck();
                var newIgnoreBottom = EditorGUILayout.FloatField(ContentBottom, ignoreBottomProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Ignore Bottom");
                    ignoreBottomProp.floatValue = Mathf.Max(0f, newIgnoreBottom);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var newIgnoreLeft = EditorGUILayout.FloatField(ContentLeft, ignoreLeftProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Ignore Left");
                    ignoreLeftProp.floatValue = Mathf.Max(0f, newIgnoreLeft);
                }

                GUILayout.Space(20);

                EditorGUI.BeginChangeCheck();
                var newIgnoreRight = EditorGUILayout.FloatField(ContentRight, ignoreRightProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Ignore Right");
                    ignoreRightProp.floatValue = Mathf.Max(0f, newIgnoreRight);
                }
            }

            EditorGUI.EndDisabledGroup();
            EditorGUIUtility.labelWidth = oriLabelWidth;
        }

        private void DrawSearchRangeSection()
        {
            var minValue = 0f;
            var maxValue = 1f;

            var oriLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 50;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var newSearchTop = EditorGUILayout.Slider(ContentTop, searchRangeUpProp.floatValue, minValue, maxValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Search Range Top");
                    searchRangeUpProp.floatValue = newSearchTop;
                }

                GUILayout.Space(20);

                EditorGUI.BeginChangeCheck();
                var newSearchBottom = EditorGUILayout.Slider(ContentBottom, searchRangeDownProp.floatValue, minValue, maxValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Search Range Bottom");
                    searchRangeDownProp.floatValue = newSearchBottom;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var newSearchLeft = EditorGUILayout.Slider(ContentLeft, searchRangeLeftProp.floatValue, minValue, maxValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Search Range Left");
                    searchRangeLeftProp.floatValue = newSearchLeft;
                }

                GUILayout.Space(20);

                EditorGUI.BeginChangeCheck();
                var newSearchRight = EditorGUILayout.Slider(ContentRight, searchRangeRightProp.floatValue, minValue, maxValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Search Range Right");
                    searchRangeRightProp.floatValue = newSearchRight;
                }
            }

            EditorGUIUtility.labelWidth = oriLabelWidth;
        }

        private void DrawFallbackNavigationsSection()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fallbackNavigationsUpProp, ContentUp);
            EditorGUILayout.PropertyField(fallbackNavigationsDownProp, ContentDown);
            EditorGUILayout.PropertyField(fallbackNavigationsLeftProp, ContentLeft);
            EditorGUILayout.PropertyField(fallbackNavigationsRightProp, ContentRight);
            EditorGUI.indentLevel--;
        }

        private void RefreshAvailableGroups()
        {
            availableGroups.Clear();

            var globalSettings = BNavSettingsLoader.GlobalSettings;
            if (globalSettings != null)
            {
                globalSettings.GetAllGroupNames(ref availableGroups);
            }

            availableGroups.Insert(0, "<none>");  // Empty option

            groupOptions = availableGroups.ToArray();
        }

        #endregion Draw Inspector Sections

        #region Draw Scene GUI

        private void DrawIgnoreRangeVisualization()
        {
            var ignoreRangeMode = targetBNav.IgnoreRangeMode;

            // Don't draw visualization if disabled
            if (ignoreRangeMode == IgnoreRangeMode.Disabled)
            {
                return;
            }

            // Don't draw visualization if auto-sync and rotation is identity
            if (ignoreRangeMode == IgnoreRangeMode.AutoSyncToSize &&
                targetBNav.transform.rotation == Quaternion.identity)
            {
                return;
            }

            if (targetBNav.IgnoreTop <= 0 &&
                targetBNav.IgnoreBottom <= 0 &&
                targetBNav.IgnoreLeft <= 0 &&
                targetBNav.IgnoreRight <= 0)
            {
                return;
            }

            var position = targetBNav.GetWorldPosition();

            // Convert ignore ranges to world space
            Vector3 topOffset, bottomOffset, leftOffset, rightOffset;

            if (targetBNav.DirectionMode == DirectionMode.Global)
            {
                // Global mode: use screen directions
                topOffset = Vector3.up * targetBNav.IgnoreTop;
                bottomOffset = Vector3.down * targetBNav.IgnoreBottom;
                leftOffset = Vector3.left * targetBNav.IgnoreLeft;
                rightOffset = Vector3.right * targetBNav.IgnoreRight;
            }
            else
            {
                // Local mode: use object's transform directions
                var t = targetBNav.CachedTransform;
                topOffset = t.up * targetBNav.IgnoreTop;
                bottomOffset = -t.up * targetBNav.IgnoreBottom;
                leftOffset = -t.right * targetBNav.IgnoreLeft;
                rightOffset = t.right * targetBNav.IgnoreRight;
            }

            // Calculate rectangle corners
            var topLeft = position + topOffset + leftOffset;
            var topRight = position + topOffset + rightOffset;
            var bottomLeft = position + bottomOffset + leftOffset;
            var bottomRight = position + bottomOffset + rightOffset;

            // Draw ignore range rectangle
            var rectanglePoints = new Vector3[] { topLeft, topRight, bottomRight, bottomLeft };
            Handles.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency
            Handles.DrawSolidRectangleWithOutline(rectanglePoints,
                new Color(1f, 0.5f, 0f, 0.1f),
                new Color(1f, 0.5f, 0f, 0.8f));

            // Draw range lines
            Handles.color = new Color(1f, 0f, 0f, 0.3f); // 半透明紅色
            var dotSize = HandleUtility.GetHandleSize(position) * 0.05f;
            if (targetBNav.IgnoreTop > 0)
            {
                Handles.DotHandleCap(0, position + topOffset, Quaternion.identity, dotSize, EventType.Repaint);
            }

            if (targetBNav.IgnoreBottom > 0)
            {
                Handles.DotHandleCap(0, position + bottomOffset, Quaternion.identity, dotSize, EventType.Repaint);
            }

            if (targetBNav.IgnoreLeft > 0)
            {
                Handles.DotHandleCap(0, position + leftOffset, Quaternion.identity, dotSize, EventType.Repaint);
            }

            if (targetBNav.IgnoreRight > 0)
            {
                Handles.DotHandleCap(0, position + rightOffset, Quaternion.identity, dotSize, EventType.Repaint);
            }
        }

        private void DrawSearchRangeVisualization()
        {
            var position = targetBNav.GetWorldPosition();
            var handleSize = HandleUtility.GetHandleSize(position);

            // Draw search cones for each enabled direction
            if (targetBNav.EnableUp)
            {
                DrawSearchCone(NavigationDirection.Up, Color.green, position, handleSize);
            }

            if (targetBNav.EnableDown)
            {
                DrawSearchCone(NavigationDirection.Down, Color.yellow, position, handleSize);
            }

            if (targetBNav.EnableLeft)
            {
                DrawSearchCone(NavigationDirection.Left, Color.blue, position, handleSize);
            }

            if (targetBNav.EnableRight)
            {
                DrawSearchCone(NavigationDirection.Right, Color.red, position, handleSize);
            }
        }

        private void DrawSearchCone(NavigationDirection direction, Color color, Vector3 center, float handleSize)
        {
            var searchRange = GetSearchRangeForDirection(direction);
            if (searchRange < 0f)
            {
                return;
            }

            // Get the ignore edge center as the cone origin
            var coneOrigin = GetIgnoreEdgeCenterWorldPosition(direction);
            var directionVector = GetDirectionVectorForVisualization(direction);

            // Calculate cone angle from search range (dot product)
            // dot = cos(angle), so angle = acos(dot)
            var coneAngle = Mathf.Acos(1f - searchRange) * Mathf.Rad2Deg;
            var coneRadius = handleSize * 0.5f;

            var leftEdge = Quaternion.AngleAxis(-coneAngle, Vector3.forward) * directionVector;
            var rightEdge = Quaternion.AngleAxis(coneAngle, Vector3.forward) * directionVector;

            var leftPoint = coneOrigin + leftEdge * coneRadius;
            var rightPoint = coneOrigin + rightEdge * coneRadius;

            // Draw cone lines
            Handles.color = new Color(color.r, color.g, color.b, 0.3f);
            Handles.DrawLine(coneOrigin, leftPoint);
            Handles.DrawLine(coneOrigin, rightPoint);

            // Draw cone arc
            var arcCenter = coneOrigin;
            var arcNormal = Vector3.forward;
            var arcFrom = leftEdge;

            Handles.color = new Color(color.r, color.g, color.b, 0.15f);
            Handles.DrawSolidArc(arcCenter, arcNormal, arcFrom, coneAngle * 2f, coneRadius);
        }

        private void DrawNavigationConnections()
        {
            DrawDirectionConnection(NavigationDirection.Up, Color.green);
            DrawDirectionConnection(NavigationDirection.Down, Color.yellow);
            DrawDirectionConnection(NavigationDirection.Left, Color.blue);
            DrawDirectionConnection(NavigationDirection.Right, Color.red);
        }

        private void DrawDirectionConnection(NavigationDirection direction, Color connectionColor)
        {
            // Find the best navigation target in this direction
            var target = targetBNav.FindNavigationTarget(direction) ?? targetBNav.FindFallbackNavigationTarget(direction);
            if (target == null)
            {
                return;
            }

            // Get source position from ignore range edge center (converted to world space)
            var sourcePosition = GetIgnoreEdgeCenterWorldPosition(direction);
            var targetPosition = target.GetWorldPosition();

            // Draw connection line
            Handles.color = connectionColor;
            Handles.DrawLine(sourcePosition, targetPosition);

            // Draw arrow head
            var direction3D = (targetPosition - sourcePosition).normalized;
            var arrowHead = targetPosition - direction3D * 0.2f;
            var perpendicular = Vector3.Cross(direction3D, Vector3.forward).normalized * 0.1f;

            Handles.DrawLine(targetPosition, arrowHead + perpendicular);
            Handles.DrawLine(targetPosition, arrowHead - perpendicular);

            // Draw direction label at source position
            var labelOffset = GetDirectionLabelOffset(direction) * 0.3f;
            var labelPosition = sourcePosition + labelOffset;
            var labelStyle = new GUIStyle()
            {
                normal = { textColor = connectionColor },
                fontSize = 10,
                fontStyle = FontStyle.Bold,
            };

            Handles.color = connectionColor;
            Handles.Label(labelPosition, direction.ToString(), labelStyle);

            // Draw target object and group name
            var targetLabelPosition = targetPosition - labelOffset;
            var targetLabelText = $"{target.name} ({target.BelongGroup})";
            Handles.Label(targetLabelPosition, $"{targetLabelText}", labelStyle);

            // Draw target label background
            var handleSize = HandleUtility.GetHandleSize(targetPosition);
            var targetLabelSize = 0.015f * handleSize * labelStyle.CalcSize(new GUIContent(targetLabelText));
            var paddingX = 0f;
            var paddingY = 2f;
            var targetLabelRect = new Rect(
                targetLabelPosition.x - targetLabelSize.x * 0.1f - paddingX,
                targetLabelPosition.y - targetLabelSize.y * 0.9f - paddingY,
                targetLabelSize.x + paddingX * 2f,
                targetLabelSize.y + paddingY * 2f);

            Handles.color = Color.white;
            Handles.DrawSolidRectangleWithOutline(targetLabelRect, new Color(1f, 1f, 1f, 0.1f), Color.clear);
        }

        private Vector3 GetIgnoreEdgeCenterWorldPosition(NavigationDirection direction)
        {
            var position = targetBNav.GetWorldPosition();

            // If ignore range is disabled, return the object's position
            if (targetBNav.IgnoreRangeMode == IgnoreRangeMode.Disabled)
            {
                return position;
            }

            if (targetBNav.DirectionMode == DirectionMode.Global)
            {
                // Global mode: use screen directions
                switch (direction)
                {
                    case NavigationDirection.Up:
                        return position + Vector3.up * targetBNav.IgnoreTop;

                    case NavigationDirection.Down:
                        return position + Vector3.down * targetBNav.IgnoreBottom;

                    case NavigationDirection.Left:
                        return position + Vector3.left * targetBNav.IgnoreLeft;

                    case NavigationDirection.Right:
                        return position + Vector3.right * targetBNav.IgnoreRight;

                    default:
                        return position;
                }
            }

            // Local mode: use object's transform directions
            var t = targetBNav.CachedTransform;
            var worldOffset = Vector3.zero;

            switch (direction)
            {
                case NavigationDirection.Up:
                    worldOffset = t.up * targetBNav.IgnoreTop;
                    break;

                case NavigationDirection.Down:
                    worldOffset = -t.up * targetBNav.IgnoreBottom;
                    break;

                case NavigationDirection.Left:
                    worldOffset = -t.right * targetBNav.IgnoreLeft;
                    break;

                case NavigationDirection.Right:
                    worldOffset = t.right * targetBNav.IgnoreRight;
                    break;

                default:
                    worldOffset = Vector3.zero;
                    break;
            }

            return position + worldOffset;
        }

        private Vector3 GetDirectionLabelOffset(NavigationDirection direction)
        {
            switch (direction)
            {
                case NavigationDirection.Up:
                    return Vector3.up;

                case NavigationDirection.Down:
                    return Vector3.down;

                case NavigationDirection.Left:
                    return Vector3.left;

                case NavigationDirection.Right:
                    return Vector3.right;

                default:
                    return Vector3.zero;
            }
        }

        private float GetSearchRangeForDirection(NavigationDirection direction)
        {
            switch (direction)
            {
                case NavigationDirection.Up:
                    return targetBNav.SearchRangeUp;

                case NavigationDirection.Down:
                    return targetBNav.SearchRangeDown;

                case NavigationDirection.Left:
                    return targetBNav.SearchRangeLeft;

                case NavigationDirection.Right:
                    return targetBNav.SearchRangeRight;

                default:
                    return 0.5f;
            }
        }

        private Vector3 GetDirectionVectorForVisualization(NavigationDirection direction)
        {
            if (targetBNav.DirectionMode == DirectionMode.Global)
            {
                // Global mode: use screen directions
                switch (direction)
                {
                    case NavigationDirection.Up:
                        return Vector3.up;

                    case NavigationDirection.Down:
                        return Vector3.down;

                    case NavigationDirection.Left:
                        return Vector3.left;

                    case NavigationDirection.Right:
                        return Vector3.right;

                    default:
                        return Vector3.zero;
                }
            }

            // Local mode: use object's transform directions
            var t = targetBNav.CachedTransform;
            switch (direction)
            {
                case NavigationDirection.Up:
                    return t.up;

                case NavigationDirection.Down:
                    return -t.up;

                case NavigationDirection.Left:
                    return -t.right;

                case NavigationDirection.Right:
                    return t.right;

                default:
                    return Vector3.zero;
            }
        }

        #endregion Draw Scene GUI
    }
}