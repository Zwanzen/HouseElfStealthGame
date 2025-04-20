using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class PathTool : EditorWindow
{
    [MenuItem("Tools/Path/Path Tool")]
    public static void ShowExample()
    {
        PathTool wnd = GetWindow<PathTool>();
        wnd.titleContent = new GUIContent("Path Tool");
    }

    // Core state
    private NPCPath _selectedPath;
    private int _selectedWaypointIndex = -1;
    private VisualElement _root;

    // Helper components
    private PathRenderer _renderer;
    private PathOperations _operations;
    private PathToolUIBuilder _uiBuilder;

    private void OnEnable()
    {
        _renderer = new PathRenderer();
        _operations = new PathOperations();
        _uiBuilder = new PathToolUIBuilder();
    
        // Subscribe to UI builder events
        _uiBuilder.OnPathSelected += OnPathSelected;
        _uiBuilder.OnSelectWaypoint += OnWaypointSelected;
        _uiBuilder.OnAddWaypoint += OnAddWaypoint;
        _uiBuilder.OnAddWaypointRelative += OnAddWaypointRelative; // Add this line
        _uiBuilder.OnRemoveWaypoint += OnRemoveWaypoint;
        _uiBuilder.OnFlipPath += OnFlipPath;
        _uiBuilder.OnSaveChanges += OnSaveChanges;
        _uiBuilder.OnGoBack += OnGoBack;

        SceneView.duringSceneGui += OnSceneGUI;
    }

    // Add this method to handle relative waypoint addition
    private void OnAddWaypointRelative(bool addBefore)
    {
        _operations.AddWaypointRelativeToSelection(_selectedPath, _selectedWaypointIndex, addBefore, out int newIndex);
        _selectedWaypointIndex = newIndex;
        BuildPathDetailsUI();
        SceneView.RepaintAll();
    }

    // Update OnDisable to unsubscribe from the new event
    private void OnDisable()
    {
        if (_uiBuilder != null)
        {
            _uiBuilder.OnPathSelected -= OnPathSelected;
            _uiBuilder.OnSelectWaypoint -= OnWaypointSelected;
            _uiBuilder.OnAddWaypoint -= OnAddWaypoint;
            _uiBuilder.OnAddWaypointRelative -= OnAddWaypointRelative; // Add this line
            _uiBuilder.OnRemoveWaypoint -= OnRemoveWaypoint;
            _uiBuilder.OnFlipPath -= OnFlipPath;
            _uiBuilder.OnSaveChanges -= OnSaveChanges;
            _uiBuilder.OnGoBack -= OnGoBack;
        }

        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public void CreateGUI()
    {
        _root = rootVisualElement;
        BuildInitialUI();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_selectedPath == null || _selectedPath.Waypoints == null)
            return;

        // Call RenderPath with isEditable set to false
        _renderer.RenderPath(_selectedPath, _selectedWaypointIndex, sceneView, OnWaypointSelected, true);
        HandleKeyboardShortcuts();
    }
    
    private void HandleKeyboardShortcuts()
    {
        Event e = Event.current;
    
        // Only process keyboard events
        if (e.type != EventType.KeyDown || _selectedWaypointIndex < 0)
            return;
        
        // Add waypoint before selected: Ctrl+B or PageUp
        if ((e.control && e.keyCode == KeyCode.B) || e.keyCode == KeyCode.PageUp)
        {
            OnAddWaypointRelative(true);
            e.Use();
        }
        // Add waypoint after selected: Ctrl+N or PageDown
        else if ((e.control && e.keyCode == KeyCode.N) || e.keyCode == KeyCode.PageDown)
        {
            OnAddWaypointRelative(false);
            e.Use();
        }
        // Go to previous waypoint: b or Keypad 4
        else if (e.keyCode == KeyCode.B || e.keyCode == KeyCode.Keypad4)
        {
            GotoPreviousWaypoint();
            e.Use();
        }
        // Go to next waypoint: n or Keypad 6
        else if (e.keyCode == KeyCode.N || e.keyCode == KeyCode.Keypad6)
        {
            GotoNextWaypoint();
            e.Use();
        }
    }
    
    private void GotoPreviousWaypoint()
    {
        if (_selectedPath != null && _selectedPath.Waypoints != null && _selectedPath.Waypoints.Length > 0)
        {
            int newIndex = _selectedWaypointIndex <= 0
                ? _selectedPath.Waypoints.Length - 1
                : _selectedWaypointIndex - 1;
            
            OnWaypointSelected(newIndex);
            SceneView.RepaintAll();
        }
    }

    private void GotoNextWaypoint()
    {
        if (_selectedPath != null && _selectedPath.Waypoints != null && _selectedPath.Waypoints.Length > 0)
        {
            int newIndex = _selectedWaypointIndex >= _selectedPath.Waypoints.Length - 1
                ? 0
                : _selectedWaypointIndex + 1;
            
            OnWaypointSelected(newIndex);
            SceneView.RepaintAll();
        }
    }
    
    private void OnGoBack()
    {
        // Reset the path data
        _selectedPath = null;
        _selectedWaypointIndex = -1;
        BuildInitialUI();
        SceneView.RepaintAll();
    }

    private void BuildInitialUI()
    {
        _uiBuilder.BuildInitialUI(_root);
    }

    private void BuildPathDetailsUI()
    {
        _uiBuilder.BuildPathDetailsUI(_root, _selectedPath, _selectedWaypointIndex);
    }

    // Event handlers
    private void OnPathSelected(NPCPath path)
    {
        // Clear any currently selected objects in the scene
        Selection.activeObject = null;
    
        _selectedPath = path;
        _selectedWaypointIndex = -1;
        BuildPathDetailsUI();
        SceneView.RepaintAll();
    }

    private void OnWaypointSelected(int index)
    {
        _selectedWaypointIndex = index;
        BuildPathDetailsUI();
        SceneView.RepaintAll();
    }

    private void OnAddWaypoint()
    {
        _operations.AddWaypoint(_selectedPath, _selectedWaypointIndex, out int newIndex);
        _selectedWaypointIndex = newIndex;
        BuildPathDetailsUI();
        SceneView.RepaintAll();
    }

    private void OnRemoveWaypoint(int index)
    {
        // Store the length before deletion
        int oldLength = _selectedPath.Waypoints.Length;
    
        // Keep track of what index to select next
        int newIndex = index - 1; // Default to previous waypoint
    
        // If we're deleting the first waypoint, select the new first waypoint
        if (newIndex < 0)
        {
            if (oldLength > 1) // If there are other waypoints left
                newIndex = 0;
            else
                newIndex = -1; // No waypoints left
        }
    
        // Record undo and delete the waypoint
        Undo.RecordObject(_selectedPath, "Delete Waypoint");
        _operations.RemoveWaypoint(_selectedPath, index);
    
        // Update the selection
        _selectedWaypointIndex = newIndex;
    
        // If there are no more waypoints, deselect
        if (_selectedPath.Waypoints.Length == 0)
            _selectedWaypointIndex = -1;
    
        // Make sure index is in valid range
        _selectedWaypointIndex = Mathf.Clamp(_selectedWaypointIndex, -1, _selectedPath.Waypoints.Length - 1);
    
        BuildPathDetailsUI();
        SceneView.RepaintAll();
    }

    private void OnFlipPath()
    {
        _operations.FlipPathDirection(_selectedPath, _selectedWaypointIndex, out int newIndex);
        _selectedWaypointIndex = newIndex;
        BuildPathDetailsUI();
        SceneView.RepaintAll();
    }

    private void OnSaveChanges()
    {
        if (_selectedPath != null && _selectedPath.Waypoints != null)
        {
            // Update Positions array with waypoint positions
            Vector3[] positions = new Vector3[_selectedPath.Waypoints.Length];
            for (int i = 0; i < _selectedPath.Waypoints.Length; i++)
            {
                positions[i] = _selectedPath.Waypoints[i].Point;
            }
        
            // Assign the positions array to the path
            _selectedPath.Positions = positions;
        
            // Mark the path as dirty to ensure changes are saved
            EditorUtility.SetDirty(_selectedPath);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}