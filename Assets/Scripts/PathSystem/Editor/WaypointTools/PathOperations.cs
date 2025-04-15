using UnityEditor;
using UnityEngine;

public class PathOperations
{
    public void AddWaypoint(NpcPath path, int selectedIndex, out int newIndex)
    {
        Vector3 newPosition = Vector3.zero;
        int insertIndex = -1;

        // If no waypoints exist yet
        if (path.Waypoints == null || path.Waypoints.Length == 0)
        {
            // Just create a new array with one waypoint
            Waypoint newWaypoint = new Waypoint
            {
                Point = newPosition,
                HasStop = false,
                HasDirection = false,
                Direction = Vector3.forward
            };

            path.Waypoints = new Waypoint[] { newWaypoint };
            newIndex = 0;
        }
        else
        {
            // Determine position and insert index based on selection
            if (selectedIndex >= 0 && selectedIndex < path.Waypoints.Length)
            {
                // Use position of selected waypoint
                newPosition = path.Waypoints[selectedIndex].Point;
                // Insert after the selected waypoint
                insertIndex = selectedIndex + 1;
            }
            else
            {
                // No selection, use position of last waypoint and append
                newPosition = path.Waypoints[^1].Point;
                insertIndex = path.Waypoints.Length;
            }

            // Create the new waypoint
            Waypoint newWaypoint = new Waypoint
            {
                Point = newPosition,
                HasStop = false,
                HasDirection = false,
                Direction = Vector3.forward
            };

            // Insert the waypoint at the determined position
            Waypoint[] currentWaypoints = path.Waypoints;
            Waypoint[] newWaypoints = new Waypoint[currentWaypoints.Length + 1];

            // Copy the waypoints before insertion point
            System.Array.Copy(currentWaypoints, 0, newWaypoints, 0, insertIndex);

            // Insert new waypoint
            newWaypoints[insertIndex] = newWaypoint;

            // Copy remaining waypoints after insertion point
            if (insertIndex < currentWaypoints.Length)
            {
                System.Array.Copy(currentWaypoints, insertIndex, newWaypoints, insertIndex + 1, currentWaypoints.Length - insertIndex);
            }

            // Update the path
            path.Waypoints = newWaypoints;
            newIndex = insertIndex;
        }

        EditorUtility.SetDirty(path);
    }

    public void AddWaypointRelativeToSelection(NpcPath path, int selectedIndex, bool addBefore, out int newIndex)
    {
        Vector3 newPosition = Vector3.zero;
        int insertIndex = -1;

        // If no waypoints exist yet
        if (path.Waypoints == null || path.Waypoints.Length == 0)
        {
            // Just create a new array with one waypoint
            Waypoint newWaypoint = new Waypoint
            {
                Point = newPosition,
                HasStop = false,
                HasDirection = false,
                Direction = Vector3.forward
            };

            path.Waypoints = new Waypoint[] { newWaypoint };
            newIndex = 0;
        }
        else
        {
            // Determine position and insert index based on selection
            if (selectedIndex >= 0 && selectedIndex < path.Waypoints.Length)
            {
                // Use position of selected waypoint
                newPosition = path.Waypoints[selectedIndex].Point;
                
                // Insert before or after the selected waypoint based on parameter
                insertIndex = addBefore ? selectedIndex : selectedIndex + 1;
            }
            else
            {
                // No selection, use position of last waypoint and append
                newPosition = path.Waypoints[^1].Point;
                insertIndex = path.Waypoints.Length;
            }

            // Create the new waypoint
            Waypoint newWaypoint = new Waypoint
            {
                Point = newPosition,
                HasStop = false,
                HasDirection = false,
                Direction = Vector3.forward
            };

            // Insert the waypoint at the determined position
            Waypoint[] currentWaypoints = path.Waypoints;
            Waypoint[] newWaypoints = new Waypoint[currentWaypoints.Length + 1];

            // Copy the waypoints before insertion point
            System.Array.Copy(currentWaypoints, 0, newWaypoints, 0, insertIndex);

            // Insert new waypoint
            newWaypoints[insertIndex] = newWaypoint;

            // Copy remaining waypoints after insertion point
            if (insertIndex < currentWaypoints.Length)
            {
                System.Array.Copy(currentWaypoints, insertIndex, newWaypoints, insertIndex + 1, currentWaypoints.Length - insertIndex);
            }

            // Update the path
            path.Waypoints = newWaypoints;
            newIndex = insertIndex;
        }

        EditorUtility.SetDirty(path);
    }
    
    public void RemoveWaypoint(NpcPath path, int index)
    {
        if (index < 0 || index >= path.Waypoints.Length)
            return;
            
        Waypoint[] waypoints = path.Waypoints;
        Waypoint[] newWaypoints = new Waypoint[waypoints.Length - 1];

        System.Array.Copy(waypoints, 0, newWaypoints, 0, index);
        System.Array.Copy(waypoints, index + 1, newWaypoints, index, waypoints.Length - index - 1);

        path.Waypoints = newWaypoints;
        EditorUtility.SetDirty(path);
    }

    public void FlipPathDirection(NpcPath path, int selectedIndex, out int newIndex)
    {
        newIndex = -1;
        
        if (path == null || path.Waypoints == null || path.Waypoints.Length <= 1)
            return;

        Undo.RecordObject(path, "Flip Path Direction");

        // Create a new array with reversed order
        Waypoint[] flippedWaypoints = new Waypoint[path.Waypoints.Length];

        for (int i = 0; i < path.Waypoints.Length; i++)
        {
            // Get the waypoint from the opposite end
            Waypoint originalWaypoint = path.Waypoints[path.Waypoints.Length - 1 - i];

            // Copy the waypoint
            Waypoint flippedWaypoint = originalWaypoint;

            flippedWaypoints[i] = flippedWaypoint;
        }

        // Update the path
        path.Waypoints = flippedWaypoints;

        // Update selected waypoint index if one is selected
        if (selectedIndex >= 0)
        {
            newIndex = path.Waypoints.Length - 1 - selectedIndex;
        }

        EditorUtility.SetDirty(path);
    }
}