using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PathVizTool : EditorWindow
{
    [MenuItem("Tools/Path/Path Viz")]
    public static void ShowWindow()
    {
        PathVizTool wnd = GetWindow<PathVizTool>();
        wnd.titleContent = new GUIContent("Path Viz");
    }

    // Core state
    private NPCPath _selectedPath;
    private int _selectedWaypointIndex = -1;
    private VisualElement _root;
    private List<NPCPath> _availablePaths = new List<NPCPath>();

    // Helper components
    private PathRenderer _renderer;
    
    private void OnEnable()
    {
        _renderer = new PathRenderer();
        SceneView.duringSceneGui += OnSceneGUI;
        LoadAvailablePaths();
    }
    
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    public void CreateGUI()
    {
        _root = rootVisualElement;
        BuildUI();
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (_selectedPath == null || _selectedPath.Waypoints == null)
            return;

        // Draw path in view-only mode without event handling
        DrawPathVisualization(_selectedPath, _selectedWaypointIndex, sceneView);
    }
    
    private void DrawPathVisualization(NPCPath path, int selectedWaypointIndex, SceneView sceneView)
    {
        // Draw path lines
        DrawPathLines(path);
    
        // Draw waypoint visualization elements
        for (int i = 0; i < path.Waypoints.Length; i++)
        {
            DrawWaypointVisual(path.Waypoints[i], i == selectedWaypointIndex);
        
            if (path.Waypoints[i].HasStop && path.Waypoints[i].HasDirection)
            {
                DrawWaypointDirection(path.Waypoints[i], i == selectedWaypointIndex);
            }
        }
    }
    
    private void DrawPathLines(NPCPath path)
    {
        if (path.Waypoints.Length < 2)
            return;

        // Draw regular path segments
        Handles.color = new Color(0f, 1f, 1f, 0.3f);
        for (int i = 0; i < path.Waypoints.Length - 1; i++)
        {
            Handles.DrawLine(path.Waypoints[i].Point, path.Waypoints[i + 1].Point, 2f);
        }

        // Draw loop line if enabled
        if (path.IsLoop && path.Waypoints.Length > 1)
        {
            Handles.color = new Color(0.8f, 0.8f, 1f, 0.3f);
            Handles.DrawLine(path.Waypoints[^1].Point, path.Waypoints[0].Point, 2f);
        }
    }
    
    private void DrawWaypointVisual(Waypoint waypoint, bool isSelected)
    {
        float handleSize = Mathf.Min(HandleUtility.GetHandleSize(waypoint.Point), 2f);
    
        // Make selected waypoints larger (0.15f) than non-selected waypoints (0.1f)
        float dotSize = Mathf.Min((isSelected ? 0.15f : 0.1f) * handleSize, 
            isSelected ? 0.15f : 0.1f);

        // Set color based on waypoint state
        if (isSelected)
            Handles.color = Color.white;
        else if (waypoint.HasAnimation)
            Handles.color = new Color(0f, 1f, 0.5f);
        else if(waypoint.HasStop)
            Handles.color = new Color(1f, 0.5f, 0f);
        else
            Handles.color = new Color(0f, 1f, 1f, 1f);

        // Draw dot
        Handles.DotHandleCap(0, waypoint.Point, Quaternion.identity, dotSize, EventType.Repaint);
    }
    
    private void DrawWaypointDirection(Waypoint waypoint, bool isSelected)
    {
        if (isSelected)
            Handles.color = Color.white;
        else
            Handles.color = Color.yellow;
        
        float handleSize = Mathf.Min(HandleUtility.GetHandleSize(waypoint.Point), 1.5f);
        Vector3 direction = waypoint.Direction.normalized;
    
        // Draw direction arrow
        Handles.DrawLine(waypoint.Point, waypoint.Point + direction * handleSize, 2f);
    }
    
    private void GotoPreviousWaypoint()
    {
        if (_selectedPath != null && _selectedPath.Waypoints != null && _selectedPath.Waypoints.Length > 0)
        {
            int newIndex = _selectedWaypointIndex <= 0
                ? _selectedPath.Waypoints.Length - 1
                : _selectedWaypointIndex - 1;

            OnWaypointSelected(newIndex);
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
        }
    }
    
    private void FocusOnSelectedWaypoint(SceneView sceneView)
    {
        if (sceneView != null && _selectedPath != null && _selectedWaypointIndex >= 0 && 
            _selectedWaypointIndex < _selectedPath.Waypoints.Length)
        {
            Vector3 focusPoint = _selectedPath.Waypoints[_selectedWaypointIndex].Point;
            sceneView.Frame(new Bounds(focusPoint, Vector3.one * 3f), false);
        }
    }

    private void LoadAvailablePaths()
    {
        _availablePaths.Clear();
        string[] guids = AssetDatabase.FindAssets("t:NpcPath");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            NPCPath npcPath = AssetDatabase.LoadAssetAtPath<NPCPath>(path);
            if (npcPath != null)
            {
                _availablePaths.Add(npcPath);
            }
        }
    }
    
    private void BuildUI()
    {
        _root.Clear();
        
        // Create toolbar with refresh button
        var toolbar = new Toolbar();
        var refreshButton = new ToolbarButton(() => {
            LoadAvailablePaths();
            BuildUI();
        });
        refreshButton.text = "Refresh";
        toolbar.Add(refreshButton);
        _root.Add(toolbar);
        
        // Title
        var titleLabel = new Label("Path Visualization Tool");
        titleLabel.style.fontSize = 16;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginTop = 10;
        titleLabel.style.marginBottom = 10;
        _root.Add(titleLabel);
        
        // Path selection section
        var pathSelectionContainer = new Box();
        pathSelectionContainer.style.marginBottom = 10;
        
        var selectionLabel = new Label("Select a Path:");
        selectionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        pathSelectionContainer.Add(selectionLabel);
        
        if (_availablePaths.Count == 0)
        {
            var noPathsLabel = new Label("No paths found. Create paths using the Path Tool first.");
            noPathsLabel.style.color = Color.yellow;
            pathSelectionContainer.Add(noPathsLabel);
        }
        else
        {
            foreach (var path in _availablePaths)
            {
                var pathButton = new Button(() => {
                    SelectPath(path);
                });
                
                pathButton.text = $"{path.name} ({path.Waypoints?.Length ?? 0} waypoints)";
                
                if (_selectedPath == path)
                {
                    pathButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
                    pathButton.style.color = Color.white;
                }
                
                pathSelectionContainer.Add(pathButton);
            }
        }
        
        _root.Add(pathSelectionContainer);
        
        // Path details section (only if a path is selected)
        if (_selectedPath != null)
        {
            AddPathDetails();
        }
    }
    
    private void AddPathDetails()
    {
        var detailsContainer = new Box();
        detailsContainer.style.marginTop = 10;

        // Path header with deselect button
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.justifyContent = Justify.SpaceBetween;
        headerRow.style.alignItems = Align.Center;
    
        var pathHeader = new Label($"Path: {_selectedPath.name}");
        pathHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        pathHeader.style.fontSize = 14;
    
        var deselectButton = new Button(DeselectPath) { text = "Stop Visualizing" };
        deselectButton.style.backgroundColor = new Color(0.7f, 0.3f, 0.3f);
        deselectButton.style.color = Color.white;
    
        headerRow.Add(pathHeader);
        headerRow.Add(deselectButton);
    
        detailsContainer.Add(headerRow);

        // Rest of the path details section remains the same...
        // Path properties
        var propertiesBox = new Box();
        propertiesBox.style.marginTop = 5;
        propertiesBox.style.marginBottom = 10;
        
        propertiesBox.Add(new Label($"Is Loop: {(_selectedPath.IsLoop ? "Yes" : "No")}"));
        propertiesBox.Add(new Label($"Total Waypoints: {_selectedPath.Waypoints?.Length ?? 0}"));
        
        // Calculate path length
        float pathLength = 0f;
        if (_selectedPath.Waypoints != null && _selectedPath.Waypoints.Length > 1)
        {
            for (int i = 0; i < _selectedPath.Waypoints.Length - 1; i++)
            {
                pathLength += Vector3.Distance(_selectedPath.Waypoints[i].Point, _selectedPath.Waypoints[i + 1].Point);
            }
            
            if (_selectedPath.IsLoop && _selectedPath.Waypoints.Length > 1)
            {
                pathLength += Vector3.Distance(_selectedPath.Waypoints[^1].Point, 
                                               _selectedPath.Waypoints[0].Point);
            }
        }
        propertiesBox.Add(new Label($"Path Length: {pathLength:F2} units"));
        
        detailsContainer.Add(propertiesBox);
        
        // Waypoint navigation
        if (_selectedPath.Waypoints != null && _selectedPath.Waypoints.Length > 0)
        {
            var navContainer = new Box();
            navContainer.style.flexDirection = FlexDirection.Row;
            navContainer.style.marginBottom = 10;
            
            var prevButton = new Button(GotoPreviousWaypoint) { text = "◀ Previous" };
            var nextButton = new Button(GotoNextWaypoint) { text = "Next ▶" };
            var focusButton = new Button(() => FocusOnSelectedWaypoint(SceneView.currentDrawingSceneView)) { text = "Focus (F)" };
            
            navContainer.Add(prevButton);
            navContainer.Add(focusButton);
            navContainer.Add(nextButton);
            
            detailsContainer.Add(navContainer);
        }
        
        // Selected waypoint details
        if (_selectedWaypointIndex >= 0 && _selectedWaypointIndex < _selectedPath.Waypoints.Length)
        {
            var selectedWaypoint = _selectedPath.Waypoints[_selectedWaypointIndex];
            
            var waypointHeader = new Label($"Selected Waypoint: #{_selectedWaypointIndex}");
            waypointHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            waypointHeader.style.marginTop = 10;
            detailsContainer.Add(waypointHeader);
            
            var waypointDetails = new Box();
            waypointDetails.Add(new Label($"Position: {selectedWaypoint.Point}"));
            waypointDetails.Add(new Label($"Has Stop: {(selectedWaypoint.HasStop ? "Yes" : "No")}"));
            if (selectedWaypoint.HasStop)
            {
                waypointDetails.Add(new Label($"Stop Time: {selectedWaypoint.StopTime:F1} seconds"));
            }
            waypointDetails.Add(new Label($"Has Direction: {(selectedWaypoint.HasDirection ? "Yes" : "No")}"));
            if (selectedWaypoint.HasDirection)
            {
                waypointDetails.Add(new Label($"Direction: {selectedWaypoint.Direction}"));
            }
            waypointDetails.Add(new Label($"Has Animation: {(selectedWaypoint.HasAnimation ? "Yes" : "No")}"));
            if (selectedWaypoint.HasAnimation)
            {
                waypointDetails.Add(new Label($"Animation: {selectedWaypoint.Animation}"));
            }
            
            detailsContainer.Add(waypointDetails);
        }
        
        _root.Add(detailsContainer);
    }
    
    private void DeselectPath()
    {
        _selectedPath = null;
        _selectedWaypointIndex = -1;
        BuildUI();
        SceneView.RepaintAll();
    }
    
    private void SelectPath(NPCPath path)
    {
        _selectedPath = path;
        _selectedWaypointIndex = -1;
        BuildUI();
        SceneView.RepaintAll();
    }
    
    private void OnWaypointSelected(int index)
    {
        _selectedWaypointIndex = index;
        BuildUI();
        SceneView.RepaintAll();
    }
}