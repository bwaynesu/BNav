# BNav - Unity UI Navigation with Group Management

[![Release Version](https://img.shields.io/github/v/release/bwaynesu/BNav?include_prereleases)](https://github.com/bwaynesu/BNav/releases) [![Release Date](https://img.shields.io/github/release-date/bwaynesu/BNav.svg)](https://github.com/bwaynesu/BNav/releases)
[![Unity Version](https://img.shields.io/badge/Unity-6.0%2B-blue.svg?style=flat&logo=unity)](https://unity3d.com/get-unity/download) [![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE) [![Twitter](https://img.shields.io/twitter/follow/bwaynesu.svg?label=Follow&style=social)](https://x.com/intent/follow?screen_name=bwaynesu)

BNav is a Unity UI navigation system that **organizes UI elements into groups** and provides smart navigation between them. Perfect for creating complex UI systems like menus, settings, and inventories where you need precise control over navigation flow.

## ✨ Key Features

### 🏷️ **UI Group Management**
- **Organize UI elements into named groups** (e.g., "MainMenu", "MainMenu/Profile", "Settings")
- **Control navigation between groups** - define which groups can navigate to which other groups
- **Hierarchical group support** with parent/child relationships using "/" separator
- **Global settings for group navigation rules**

### 🎯 **Smart Navigation System**
- **Intelligent target selection** based on distance, angle, and priority
- **Customizable search cones** for each direction (0-1 range, where 0.5 ≈ 120°)
- **Ignore ranges** to exclude certain areas from navigation calculations
- **Global vs Local direction modes** (screen coordinates vs object transform)

### 🔄 **Fallback Navigation**
- **Backup navigation targets** when no direct target is found
- **Perfect for loop navigation** (e.g., last item jumps to first item)
- **Per-direction fallback lists** (Up, Down, Left, Right)

### 🎨 **Visual Development Tools**
- **Scene view gizmos** showing navigation connections and search ranges
- **Custom inspector** with group management and debugging tools
- **Real-time navigation preview** during play mode

## 🚀 Quick Start

### Installation

1. **Unity Package Manager**
    - Open `Window` > `Package Manager`
    - Click `+` > `Install package from git URL...`
    - Enter: `https://github.com/bwaynesu/BNav.git?path=Assets/BNav`

2. **Manual Installation**
    - Download the latest `.unitypackage` from the [Releases](https://github.com/bwaynesu/BNav/releases) page
    - In Unity, double-click the `.unitypackage` or use `Assets > Import Package > Custom Package...` to import

### Basic Setup

1. **Add `BNavigation` Component**
    - Attach to any GameObject with a Selectable component (Button, Toggle, etc.)

2. **Create Groups**
    - Use `Project Settings` > `BNav Settings` or open Global Settings from the `BNavigation` Component
    - Use the `Add New Group` tool to create groups
    - Assign the `Belong Group` field in the `BNavigation` Component to organize your UI elements

3. **Configure Group Navigation Rules**
    - Open the Global Settings asset
    - Define which groups can navigate to other groups

## 📖 Core Concepts

### Group Management System

The heart of BNav is organizing UI elements into groups and controlling navigation between them.

```csharp
// Example group structure:
"MainMenu"           // Main menu buttons (e.g. Play, Settings, Profile, Exit)
"MainMenu/Profile"   // Player profile group (e.g. switch character, edit nickname)
"MainMenu/Settings"  // Settings submenu group (e.g. Audio, Video, Controls)
"MainMenu/Settings/Audio"  // Audio settings group (e.g. volume slider, mute button)
"MainMenu/Settings/Video"  // Video settings group
```

### Global Settings Configuration

Create navigation rules between groups:

```csharp
// In BNavGlobalSettings:
"MainMenu" → ["MainMenu/Profile", "MainMenu/Settings"]
"MainMenu/Settings" → ["MainMenu/Settings/Audio", "MainMenu/Settings/Video"]
"MainMenu/Settings/Audio" → ["MainMenu/Settings"]
"MainMenu/Settings/Video" → ["MainMenu/Settings"]
```

### BNavigation Component

Replace Unity's default navigation with BNavigation:

```csharp
[SerializeField] private string belongGroup = "MainMenu";
[SerializeField] private int priority = 0;
[SerializeField] private bool enableUp = true;
[SerializeField] private bool enableDown = true;
[SerializeField] private bool enableLeft = true;
[SerializeField] private bool enableRight = true;
...
```

## ⚙️ Advanced Configuration

### Direction Modes

Choose between global (screen) or local (transform) coordinates:

```csharp
// Global mode: Up = screen up, regardless of rotation
navigation.DirectionMode = DirectionMode.Global;

// Local mode: Up = transform.up, follows object rotation  
navigation.DirectionMode = DirectionMode.Local;
```

### Ignore Range Configuration

Exclude areas around UI elements from navigation:

```csharp
// Disable ignore range completely
navigation.IgnoreRangeMode = IgnoreRangeMode.Disabled;

// Auto-sync with RectTransform size
navigation.IgnoreRangeMode = IgnoreRangeMode.AutoSyncToSize;

// Or manually configure
navigation.IgnoreRangeMode = IgnoreRangeMode.Manual;
navigation.IgnoreTop = 50f;
navigation.IgnoreBottom = 50f;
navigation.IgnoreLeft = 30f;
navigation.IgnoreRight = 30f;
```

### Search Range Tuning

Control the navigation search cone for each direction:

```csharp
// Narrow search cone (more precise)
navigation.SearchRangeUp = 0.3f;    // ~90° cone
navigation.SearchRangeDown = 0.3f;

// Wide search cone (more forgiving)
navigation.SearchRangeLeft = 0.7f;  // ~140° cone
navigation.SearchRangeRight = 0.7f;
```

### Fallback Navigation

Create loop navigation or backup targets:

```csharp
// Loop navigation: last item → first item
var lastItem = inventoryItems[inventoryItems.Length - 1].GetComponent<BNavigation>();
var firstItem = inventoryItems[0].GetComponent<BNavigation>();

lastItem.FallbackNavigationsDown.Add(firstItem);
firstItem.FallbackNavigationsUp.Add(lastItem);
```

## 🔧 Runtime API

### Group Management

```csharp
// Change group at runtime
navigation.BelongGroup = "NewGroup";

// Check navigation possibility
bool canNavigate = BNavManager.GlobalSettings.CanNavigate(belongGroup, targetGroup);

// Iterate all reachable navigations
foreach (var navigation in BNavManager.EachReachableNavigation(belongGroup, globalSettings))
```

### Finding Navigation Targets

```csharp
// Find target in specific direction
var target = navigation.FindNavigationTarget(NavigationDirection.Up);

// Find fallback target
var fallbackTarget = navigation.FindFallbackNavigationTarget(NavigationDirection.Down);

// Check if can navigate to specific target
bool canNavigate = navigation.CanNavigateTo(otherNavigation);
```

## 🎨 Visual Debugging

### Scene View Features

- **Colorful lines**: Available navigation connections
- **Search cones**: Visual representation of search ranges
- **Orange rectangles**: Ignore ranges

### Inspector Tools

- **Group dropdown**: Easy group assignment with validation
- **Priority settings**: Control selection preference
- **Search range sliders**: Visual tuning of navigation cones
- **Fallback lists**: Drag-and-drop backup targets

### Debug Mode

```csharp
// Enable navigation following in editor
BNavigation.debugFollowNavigation = true;
```

## 🔧 Troubleshooting

### Common Issues

**Q: "Group 'MyGroup' not found in Global Settings"**
- Use `Project Settings` > `BNav Settings` or open Global Settings from the `BNavigation` Component
- Add your group in the Global Settings editor
- Ensure the group name matches exactly

**Q: Navigation not working between groups**
- Check Global Settings - make sure source group can navigate to target group
- Add the target group to the source group's `Reachable Groups` list

**Q: UI elements not responding to navigation**
- Check if the Selectable is interactable
- Verify the `BNavigation` component is enabled

**Q: Navigation jumping to unexpected targets**
- Adjust search ranges to narrow the selection cone
- Set up ignore ranges to exclude nearby elements
- Use priority values to prefer certain targets

## 📄 License

This project is licensed under the [MIT License](LICENSE).

## ✍️ Author

* [bwaynesu](https://bwaynesu.github.io/portfolio/) [![](https://img.shields.io/twitter/follow/bwaynesu.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=bwaynesu) ![GitHub followers](https://img.shields.io/github/followers/bwaynesu?style=social)

## 🔗 Links

- [Repository](https://github.com/bwaynesu/BNav)
- [Releases](https://github.com/bwaynesu/BNav/releases)
- [Issues](https://github.com/bwaynesu/BNav/issues)
- [Discussions](https://github.com/bwaynesu/BNav/discussions)

## 📚 See Also

- [GitHub](https://bwaynesu.github.io/portfolio/)
- [Asset Store](https://assetstore.unity.com/publishers/115148)
- [Medium](https://medium.com/@bwaynesu)
- [X](https://x.com/bwaynesu)
- [YouTube](https://www.youtube.com/@bwaynesu)

---

**BNav** - Making Unity UI navigation simple and powerful through intelligent grouping 🎯
