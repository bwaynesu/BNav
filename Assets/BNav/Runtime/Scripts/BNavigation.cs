using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BTools.BNav
{
    /// <summary>
    /// Direction mode for navigation calculation
    /// </summary>
    public enum DirectionMode
    {
        /// <summary>
        /// Calculate directions based on screen coordinates (Y+ is up)
        /// </summary>
        Global,

        /// <summary>
        /// Calculate directions based on object's local transform (Transform.up vector)
        /// </summary>
        Local
    }

    /// <summary>
    /// Navigation directions
    /// </summary>
    public enum NavigationDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// Mode for ignore range calculation
    /// </summary>
    public enum IgnoreRangeMode
    {
        /// <summary>
        /// Disable ignore range - don't exclude any navigation targets based on range
        /// </summary>
        Disabled,

        /// <summary>
        /// Automatically sync ignore range values with RectTransform size
        /// </summary>
        AutoSyncToSize,

        /// <summary>
        /// Use manually configured ignore range values
        /// </summary>
        Manual
    }

    /// <summary>
    /// Enhanced navigation component that overrides Unity's built-in Selectable navigation
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Selectable))]
    public class BNavigation : MonoBehaviour, IMoveHandler
    {
#if UNITY_EDITOR

        /// <summary>
        /// Debugging: follow the navigation target in editor
        /// </summary>
        public static bool debugFollowNavigation = false;

#endif

        [Tooltip("The navigation group this component belongs to")]
        [SerializeField]
        private string belongGroup = "";

        [Tooltip("Priority for navigation selection (higher values have priority when distances are equal)")]
        [SerializeField]
        private int priority = 0;

        [Tooltip("Enable upward navigation")]
        [SerializeField]
        private bool enableUp = true;

        [Tooltip("Enable downward navigation")]
        [SerializeField]
        private bool enableDown = true;

        [Tooltip("Enable leftward navigation")]
        [SerializeField]
        private bool enableLeft = true;

        [Tooltip("Enable rightward navigation")]
        [SerializeField]
        private bool enableRight = true;

        [Tooltip("Direction calculation mode")]
        [SerializeField]
        private DirectionMode directionMode = DirectionMode.Global;

        [Tooltip("Mode for ignore range calculation")]
        [SerializeField]
        private IgnoreRangeMode ignoreRangeMode = IgnoreRangeMode.AutoSyncToSize;

        [Tooltip("Ignore range from object pivot - top")]
        [SerializeField]
        private float ignoreTop = 0f;

        [Tooltip("Ignore range from object pivot - bottom")]
        [SerializeField]
        private float ignoreBottom = 0f;

        [Tooltip("Ignore range from object pivot - left")]
        [SerializeField]
        private float ignoreLeft = 0f;

        [Tooltip("Ignore range from object pivot - right")]
        [SerializeField]
        private float ignoreRight = 0f;

        [Tooltip("Search range for upward navigation (0-1, where 0.5 = 60 degrees)")]
        [SerializeField, Range(0f, 1f)]
        private float searchRangeUp = 0.5f;

        [Tooltip("Search range for downward navigation (0-1, where 0.5 = 60 degrees)")]
        [SerializeField, Range(0f, 1f)]
        private float searchRangeDown = 0.5f;

        [Tooltip("Search range for leftward navigation (0-1, where 0.5 = 60 degrees)")]
        [SerializeField, Range(0f, 1f)]
        private float searchRangeLeft = 0.5f;

        [Tooltip("Search range for rightward navigation (0-1, where 0.5 = 60 degrees)")]
        [SerializeField, Range(0f, 1f)]
        private float searchRangeRight = 0.5f;

        [Tooltip("Fallback navigations for upward direction (used when no direct target found)")]
        [SerializeField]
        private List<BNavigation> fallbackNavigationsUp = new List<BNavigation>();

        [Tooltip("Fallback navigations for downward direction (used when no direct target found)")]
        [SerializeField]
        private List<BNavigation> fallbackNavigationsDown = new List<BNavigation>();

        [Tooltip("Fallback navigations for leftward direction (used when no direct target found)")]
        [SerializeField]
        private List<BNavigation> fallbackNavigationsLeft = new List<BNavigation>();

        [Tooltip("Fallback navigations for rightward direction (used when no direct target found)")]
        [SerializeField]
        private List<BNavigation> fallbackNavigationsRight = new List<BNavigation>();

        private Selectable cachedSelectable;
        private Transform cachedTranform;
        private RectTransform cachedRectTransform;

        #region Properties

        /// <summary>
        /// Navigation group type
        /// </summary>
        public string BelongGroup
        {
            get { return belongGroup; }
            set
            {
                if (belongGroup == value)
                {
                    return;
                }

                // Remove from old group
                BNavManager.RemoveNavigation(this);

                belongGroup = value;

                // Add to new group
                BNavManager.AddNavigation(this);
            }
        }

        /// <summary>
        /// Navigation priority
        /// </summary>
        public int Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        /// <summary>
        /// Enable upward navigation
        /// </summary>
        public bool EnableUp
        {
            get { return enableUp; }
            set { enableUp = value; }
        }

        /// <summary>
        /// Enable downward navigation
        /// </summary>
        public bool EnableDown
        {
            get { return enableDown; }
            set { enableDown = value; }
        }

        /// <summary>
        /// Enable leftward navigation
        /// </summary>
        public bool EnableLeft
        {
            get { return enableLeft; }
            set { enableLeft = value; }
        }

        /// <summary>
        /// Enable rightward navigation
        /// </summary>
        public bool EnableRight
        {
            get { return enableRight; }
            set { enableRight = value; }
        }

        /// <summary>
        /// Direction calculation mode
        /// </summary>
        public DirectionMode DirectionMode
        {
            get { return directionMode; }
            set { directionMode = value; }
        }

        /// <summary>
        /// Mode for ignore range calculation
        /// </summary>
        public IgnoreRangeMode IgnoreRangeMode
        {
            get { return ignoreRangeMode; }
            set { ignoreRangeMode = value; }
        }

        /// <summary>
        /// Ignore range - top
        /// </summary>
        public float IgnoreTop
        {
            get { return ignoreTop; }
            set { ignoreTop = value; }
        }

        /// <summary>
        /// Ignore range - bottom
        /// </summary>
        public float IgnoreBottom
        {
            get { return ignoreBottom; }
            set { ignoreBottom = value; }
        }

        /// <summary>
        /// Ignore range - left
        /// </summary>
        public float IgnoreLeft
        {
            get { return ignoreLeft; }
            set { ignoreLeft = value; }
        }

        /// <summary>
        /// Ignore range - right
        /// </summary>
        public float IgnoreRight
        {
            get { return ignoreRight; }
            set { ignoreRight = value; }
        }

        /// <summary>
        /// Search range for upward navigation
        /// </summary>
        public float SearchRangeUp
        {
            get { return searchRangeUp; }
            set { searchRangeUp = Mathf.Clamp01(value); }
        }

        /// <summary>
        /// Search range for downward navigation
        /// </summary>
        public float SearchRangeDown
        {
            get { return searchRangeDown; }
            set { searchRangeDown = Mathf.Clamp01(value); }
        }

        /// <summary>
        /// Search range for leftward navigation
        /// </summary>
        public float SearchRangeLeft
        {
            get { return searchRangeLeft; }
            set { searchRangeLeft = Mathf.Clamp01(value); }
        }

        /// <summary>
        /// Search range for rightward navigation
        /// </summary>
        public float SearchRangeRight
        {
            get { return searchRangeRight; }
            set { searchRangeRight = Mathf.Clamp01(value); }
        }

        /// <summary>
        /// Cached Selectable component
        /// </summary>
        public Selectable CachedSelectable
        {
            get
            {
                if (cachedSelectable == null)
                {
                    cachedSelectable = GetComponent<Selectable>();
                }
                return cachedSelectable;
            }
        }

        /// <summary>
        /// Cached Transform component
        /// </summary>
        public Transform CachedTransform
        {
            get
            {
                if (cachedTranform == null)
                {
                    cachedTranform = GetComponent<Transform>();
                }

                return cachedTranform;
            }
        }

        /// <summary>
        /// Cached RectTransform component
        /// </summary>
        public RectTransform CachedRectTransform
        {
            get
            {
                if (cachedRectTransform == null)
                {
                    cachedRectTransform = GetComponent<RectTransform>();
                }

                return cachedRectTransform;
            }
        }

        /// <summary>
        /// Fallback navigations for upward direction
        /// </summary>
        public List<BNavigation> FallbackNavigationsUp
        {
            get { return fallbackNavigationsUp; }
            set { fallbackNavigationsUp = value ?? new List<BNavigation>(); }
        }

        /// <summary>
        /// Fallback navigations for downward direction
        /// </summary>
        public List<BNavigation> FallbackNavigationsDown
        {
            get { return fallbackNavigationsDown; }
            set { fallbackNavigationsDown = value ?? new List<BNavigation>(); }
        }

        /// <summary>
        /// Fallback navigations for leftward direction
        /// </summary>
        public List<BNavigation> FallbackNavigationsLeft
        {
            get { return fallbackNavigationsLeft; }
            set { fallbackNavigationsLeft = value ?? new List<BNavigation>(); }
        }

        /// <summary>
        /// Fallback navigations for rightward direction
        /// </summary>
        public List<BNavigation> FallbackNavigationsRight
        {
            get { return fallbackNavigationsRight; }
            set { fallbackNavigationsRight = value ?? new List<BNavigation>(); }
        }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Get the world position of this navigation component
        /// </summary>
        /// <returns>World position</returns>
        public Vector3 GetWorldPosition()
        {
            return CachedTransform.position;
        }

        /// <summary>
        /// Get the screen position of this navigation component
        /// </summary>
        /// <returns>Screen position</returns>
        public Vector2 GetScreenPosition()
        {
            if (CachedRectTransform != null)
            {
                var worldPosition = CachedRectTransform.position;
                return RectTransformUtility.WorldToScreenPoint(null, worldPosition);
            }

            if (Camera.main != null)
            {
                return Camera.main.WorldToScreenPoint(CachedTransform.position);
            }

            return CachedTransform.position;
        }

        /// <summary>
        /// Check if this component can navigate to another component
        /// </summary>
        /// <param name="target">Target navigation component</param>
        /// <returns>True if navigation is allowed</returns>
        public bool CanNavigateTo(BNavigation target)
        {
            var globalSettings = BNavSettingsLoader.GlobalSettings;
            if (globalSettings == null || !globalSettings.CanNavigate(belongGroup, target.belongGroup))
            {
                return false;
            }

            if (target == null || target == this)
            {
                return false;
            }

            if (!target.isActiveAndEnabled || !target.CachedSelectable.IsInteractable())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handle move events from the EventSystem
        /// </summary>
        /// <param name="eventData">Move event data</param>
        public void OnMove(AxisEventData eventData)
        {
            var globalSettings = BNavSettingsLoader.GlobalSettings;
            if (globalSettings == null)
            {
                return;
            }

            if (!globalSettings.HasGroup(belongGroup))
            {
                Debug.LogWarning($"Cannot navigate from group '{gameObject.name}' because it belongs to an unknown group: {belongGroup}.", this);
                return;
            }

            // Convert move direction to navigation direction
            NavigationDirection? direction = GetNavigationDirection(eventData.moveDir);
            if (!direction.HasValue)
            {
                return;
            }

            // Find the target using BNavigation logic
            var target = FindNavigationTarget(direction.Value);

            // If no target found, try fallback navigation
            if (target == null || target.CachedSelectable == null)
            {
                target = FindFallbackNavigationTarget(direction.Value);
            }

            // If still no target found, stay at current position (don't move)
            if (target == null || target.CachedSelectable == null)
            {
                return;
            }

            // Navigate to the target
            target.CachedSelectable.Select();
            eventData.Use();

#if UNITY_EDITOR
            if (debugFollowNavigation)
            {
                UnityEditor.Selection.activeGameObject = target.gameObject;
            }
#endif
        }

        /// <summary>
        /// Find the best navigation target in a specific direction
        /// </summary>
        /// <param name="direction">Navigation direction (Up, Down, Left, Right)</param>
        /// <returns>The best navigation target, or null if none found</returns>
        public BNavigation FindNavigationTarget(NavigationDirection direction)
        {
            if (!IsDirectionEnabled(direction))
            {
                return null;
            }

            var globalSettings = BNavSettingsLoader.GlobalSettings;
            if (globalSettings == null)
            {
                return null;
            }

            var bestTarget = (BNavigation)default;
            var bestSqrDistance = float.MaxValue;
            var bestPriority = int.MinValue;

            var edgeCenterPosition = GetIgnoreEdgeCenterPosition(direction);
            var directionVector = GetDirectionVector(direction);
            var requiredDot = Mathf.Clamp(1f - GetSearchRangeForDirection(direction), 0f, 0.9999999f);

            // Iterate all reachable navigations

            foreach (var target in BNavManager.EachReachableNavigation(belongGroup, globalSettings))
            {
                if (!CanNavigateTo(target))
                {
                    continue;
                }

                // Check if target is in the correct direction
                var targetPosition = target.GetScreenPosition();
                var toTarget = (targetPosition - edgeCenterPosition).normalized;

                var dot = Vector2.Dot(directionVector, toTarget);
                if (dot <= requiredDot) // Check against direction-specific search range
                {
                    continue;
                }

                // Use squared distance for comparison
                var sqrDistance = (edgeCenterPosition - targetPosition).sqrMagnitude;

                // Compare using squared distance
                if (IsBetterTarget(sqrDistance, target.Priority, bestSqrDistance, bestPriority))
                {
                    bestTarget = target;
                    bestSqrDistance = sqrDistance;
                    bestPriority = target.Priority;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Find the best fallback navigation target in a specific direction
        /// </summary>
        /// <param name="direction">Navigation direction (Up, Down, Left, Right)</param>
        /// <returns>The best fallback navigation target, or null if none found</returns>
        public BNavigation FindFallbackNavigationTarget(NavigationDirection direction)
        {
            if (!IsDirectionEnabled(direction))
            {
                return null;
            }

            var fallbackList = GetFallbackListForDirection(direction);
            if (fallbackList == null || fallbackList.Count == 0)
            {
                return null;
            }

            var edgeCenterPosition = GetIgnoreEdgeCenterPosition(direction);
            var bestTarget = (BNavigation)null;
            var bestSqrDistance = float.MaxValue;
            var bestPriority = int.MinValue;

            foreach (var target in fallbackList)
            {
                if (!CanNavigateTo(target))
                {
                    continue;
                }

                var targetPosition = target.GetScreenPosition();
                var sqrDistance = (edgeCenterPosition - targetPosition).sqrMagnitude;

                if (IsBetterTarget(sqrDistance, target.Priority, bestSqrDistance, bestPriority))
                {
                    bestTarget = target;
                    bestSqrDistance = sqrDistance;
                    bestPriority = target.Priority;
                }
            }

            return bestTarget;
        }

        #endregion Public Methods

        #region Unity Methods

        private void OnValidate()
        {
            // Ensure we have a Selectable component
            if (GetComponent<Selectable>() == null)
            {
                Debug.LogWarning($"BNavigation component on {gameObject.name} requires a Selectable component.", this);
            }

            // Update navigation override when properties change in editor
            if (isActiveAndEnabled)
            {
                OverrideSelectableNavigation();
            }
        }

        private void OnEnable()
        {
            // Add to global manager
            BNavManager.AddNavigation(this);

            // Override Selectable navigation
            OverrideSelectableNavigation();
        }

        private void OnDisable()
        {
            // Remove from global manager
            BNavManager.RemoveNavigation(this);
        }

        private void Update()
        {
            // Auto sync ignore range with RectTransform size
            if (IgnoreRangeMode == IgnoreRangeMode.AutoSyncToSize && CachedRectTransform != null)
            {
                var size = CachedRectTransform.rect.size;
                var scale = CachedRectTransform.lossyScale;

                ignoreTop = size.y * 0.5f * scale.y;
                ignoreBottom = size.y * 0.5f * scale.y;
                ignoreLeft = size.x * 0.5f * scale.x;
                ignoreRight = size.x * 0.5f * scale.x;
            }
        }

        private void OnDestroy()
        {
            // Ensure removal from global manager
            BNavManager.RemoveNavigation(this);
        }

        #endregion Unity Methods

        /// <summary>
        /// Override the Selectable's navigation settings
        /// </summary>
        private void OverrideSelectableNavigation()
        {
            if (CachedSelectable == null)
            {
                return;
            }

            // Set navigation mode to none, we will handle navigation manually
            var navigation = CachedSelectable.navigation;
            navigation.mode = Navigation.Mode.None;

            CachedSelectable.navigation = navigation;
        }

        /// <summary>
        /// Get the search range for a specific direction
        /// </summary>
        /// <param name="direction">Navigation direction</param>
        /// <returns>Search range value (0-1)</returns>
        private float GetSearchRangeForDirection(NavigationDirection direction)
        {
            switch (direction)
            {
                case NavigationDirection.Up:
                    return SearchRangeUp;

                case NavigationDirection.Down:
                    return SearchRangeDown;

                case NavigationDirection.Left:
                    return SearchRangeLeft;

                case NavigationDirection.Right:
                    return SearchRangeRight;

                default:
                    return 0.5f;
            }
        }

        /// <summary>
        /// Check if a specific direction is enabled
        /// </summary>
        /// <param name="direction">Direction to check</param>
        /// <returns>True if direction is enabled</returns>
        private bool IsDirectionEnabled(NavigationDirection direction)
        {
            switch (direction)
            {
                case NavigationDirection.Up:
                    return EnableUp;

                case NavigationDirection.Down:
                    return EnableDown;

                case NavigationDirection.Left:
                    return EnableLeft;

                case NavigationDirection.Right:
                    return EnableRight;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Get the direction vector for a specific navigation direction
        /// </summary>
        /// <param name="direction">Navigation direction</param>
        /// <returns>Normalized direction vector</returns>
        private Vector2 GetDirectionVector(NavigationDirection direction)
        {
            if (directionMode == DirectionMode.Global)
            {
                // Global mode: use screen directions
                switch (direction)
                {
                    case NavigationDirection.Up:
                        return Vector2.up;

                    case NavigationDirection.Down:
                        return Vector2.down;

                    case NavigationDirection.Left:
                        return Vector2.left;

                    case NavigationDirection.Right:
                        return Vector2.right;

                    default:
                        return Vector2.zero;
                }
            }

            // Local mode: use object's transform directions
            var t = CachedTransform;
            var worldDirection = Vector3.zero;

            switch (direction)
            {
                case NavigationDirection.Up:
                    worldDirection = t.up;
                    break;

                case NavigationDirection.Down:
                    worldDirection = -t.up;
                    break;

                case NavigationDirection.Left:
                    worldDirection = -t.right;
                    break;

                case NavigationDirection.Right:
                    worldDirection = t.right;
                    break;

                default:
                    worldDirection = Vector3.zero;
                    break;
            }

            // For UI elements, we can directly use the transform directions projected to screen
            // since UI is typically in screen space already
            if (CachedRectTransform != null)
            {
                // For RectTransform, use the transform directions directly
                var screenDirection = (Vector2)worldDirection;
                return screenDirection.normalized;
            }

            // For 3D objects, convert world direction to screen direction
            var centerScreen = (Vector3)GetScreenPosition();
            var offsetScreen = Camera.main != null ?
                Camera.main.WorldToScreenPoint(CachedTransform.position + worldDirection) :
                centerScreen + worldDirection;

            return ((Vector2)(offsetScreen - centerScreen)).normalized;
        }

        /// <summary>
        /// Get the center position of the ignore range edge for a specific direction
        /// </summary>
        /// <param name="direction">Navigation direction</param>
        /// <returns>Edge center position in screen space</returns>
        private Vector2 GetIgnoreEdgeCenterPosition(NavigationDirection direction)
        {
            var sourcePosition = GetScreenPosition();

            // If ignore range is disabled, return the source position directly
            if (IgnoreRangeMode == IgnoreRangeMode.Disabled)
            {
                return sourcePosition;
            }

            if (directionMode == DirectionMode.Global)
            {
                // Global mode: direct screen coordinate offset
                switch (direction)
                {
                    case NavigationDirection.Up:
                        return sourcePosition + Vector2.up * ignoreTop;

                    case NavigationDirection.Down:
                        return sourcePosition + Vector2.down * ignoreBottom;

                    case NavigationDirection.Left:
                        return sourcePosition + Vector2.left * ignoreLeft;

                    case NavigationDirection.Right:
                        return sourcePosition + Vector2.right * ignoreRight;

                    default:
                        return sourcePosition;
                }
            }

            // Local mode: use object's transform directions
            var t = CachedTransform;
            var screenOffset = Vector2.zero;

            if (CachedRectTransform != null)
            {
                // For UI elements, use transform directions directly in screen space
                switch (direction)
                {
                    case NavigationDirection.Up:
                        screenOffset = new Vector2(t.up.x, t.up.y) * ignoreTop;
                        break;

                    case NavigationDirection.Down:
                        screenOffset = new Vector2(-t.up.x, -t.up.y) * ignoreBottom;
                        break;

                    case NavigationDirection.Left:
                        screenOffset = new Vector2(-t.right.x, -t.right.y) * ignoreLeft;
                        break;

                    case NavigationDirection.Right:
                        screenOffset = new Vector2(t.right.x, t.right.y) * ignoreRight;
                        break;

                    default:
                        screenOffset = Vector2.zero;
                        break;
                }

                return sourcePosition + screenOffset;
            }

            // For 3D objects, convert world offset to screen offset
            var worldOffset = Vector3.zero;

            switch (direction)
            {
                case NavigationDirection.Up:
                    worldOffset = t.up * ignoreTop;
                    break;

                case NavigationDirection.Down:
                    worldOffset = -t.up * ignoreBottom;
                    break;

                case NavigationDirection.Left:
                    worldOffset = -t.right * ignoreLeft;
                    break;

                case NavigationDirection.Right:
                    worldOffset = t.right * ignoreRight;
                    break;

                default:
                    worldOffset = Vector3.zero;
                    break;
            }

            if (Camera.main != null)
            {
                var worldEdgePos = t.position + worldOffset;
                return Camera.main.WorldToScreenPoint(worldEdgePos);
            }

            return sourcePosition + (Vector2)worldOffset;
        }

        /// <summary>
        /// Check if a target is better than the current best target
        /// </summary>
        /// <param name="distance">Distance to target</param>
        /// <param name="priority">Target priority</param>
        /// <param name="bestDistance">Current best distance</param>
        /// <param name="bestPriority">Current best priority</param>
        /// <returns>True if target is better</returns>
        private bool IsBetterTarget(float distance, int priority, float bestDistance, int bestPriority)
        {
            // Compare by distance (closer is better)
            if (distance != bestDistance)
            {
                return distance < bestDistance;
            }

            // Compare by priority (higher is better)
            return priority > bestPriority;
        }

        /// <summary>
        /// Convert Unity's MoveDirection to our NavigationDirection
        /// </summary>
        /// <param name="moveDirection">Unity's move direction</param>
        /// <returns>Our navigation direction, or null if not supported</returns>
        private NavigationDirection? GetNavigationDirection(MoveDirection moveDirection)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                    return NavigationDirection.Up;

                case MoveDirection.Down:
                    return NavigationDirection.Down;

                case MoveDirection.Left:
                    return NavigationDirection.Left;

                case MoveDirection.Right:
                    return NavigationDirection.Right;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Get the fallback navigation list for a specific direction
        /// </summary>
        /// <param name="direction">Navigation direction</param>
        /// <returns>Fallback navigation list, or null if not found</returns>
        private List<BNavigation> GetFallbackListForDirection(NavigationDirection direction)
        {
            switch (direction)
            {
                case NavigationDirection.Up:
                    return FallbackNavigationsUp;

                case NavigationDirection.Down:
                    return FallbackNavigationsDown;

                case NavigationDirection.Left:
                    return FallbackNavigationsLeft;

                case NavigationDirection.Right:
                    return FallbackNavigationsRight;

                default:
                    return null;
            }
        }
    }
}