using UnityEditor;
using UnityEngine;

public class PathOperations
{
    public void AddWaypoint(NpcPath path, int selectedIndex, out int newIndex)
    {
        // Register the operation with Undo system before making changes
        Undo.RecordObject(path, "Add Waypoint");
        
        Vector3 newPosition = Vector3.zero;
        int insertIndex = -1;
        float offsetDistance = 1.5f; // Same offset distance

        // If no waypoints exist yet (unchanged)
        if (path.Waypoints == null || path.Waypoints.Length == 0)
        {
            // Just create a new array with one waypoint
            Waypoint newWaypoint = new Waypoint
            {
                Point = newPosition,
                HasStop = false,
                HasDirection = false,
                Direction = Vector3.forward,
                StopTime = 3.0f
            };

            path.Waypoints = new Waypoint[] { newWaypoint };
            newIndex = 0;
        }
        else
        {
            // Determine position and insert index based on selection
            if (selectedIndex >= 0 && selectedIndex < path.Waypoints.Length)
            {
                // Use position of selected waypoint with offset
                newPosition = path.Waypoints[selectedIndex].Point + Vector3.forward * offsetDistance;
                insertIndex = selectedIndex + 1;
            }
            else
            {
                // No selection, use position of last waypoint with offset
                newPosition = path.Waypoints[path.Waypoints.Length - 1].Point + Vector3.forward * offsetDistance;
                insertIndex = path.Waypoints.Length;
            }

            // Create and insert the waypoint (rest unchanged)
            Waypoint newWaypoint = new Waypoint
            {
                Point = newPosition,
                HasStop = false,
                HasDirection = false,
                Direction = Vector3.forward,
                StopTime = 3.0f
            };

            Waypoint[] currentWaypoints = path.Waypoints;
            Waypoint[] newWaypoints = new Waypoint[currentWaypoints.Length + 1];

            System.Array.Copy(currentWaypoints, 0, newWaypoints, 0, insertIndex);
            newWaypoints[insertIndex] = newWaypoint;
            if (insertIndex < currentWaypoints.Length)
            {
                System.Array.Copy(currentWaypoints, insertIndex, newWaypoints, insertIndex + 1, currentWaypoints.Length - insertIndex);
            }

            path.Waypoints = newWaypoints;
            newIndex = insertIndex;
        }

        EditorUtility.SetDirty(path);
    }

    public void AddWaypointRelativeToSelection(NpcPath path, int selectedIndex, bool addBefore, out int newIndex)
    {
        // Register the operation with Undo system before making changes
        Undo.RecordObject(path, "Add Waypoint");
        
        Vector3 newPosition = Vector3.zero;
        int insertIndex = -1;
        float offsetDistance = 1.5f; // Fall-back offset distance when no neighboring waypoint exists

        // If no waypoints exist yet
        if (path.Waypoints == null || path.Waypoints.Length == 0)
        {
            // Just create a new array with one waypoint at origin
            Waypoint newWaypoint = new Waypoint
            {
                Point = Vector3.zero,
                HasStop = false,
                HasDirection = false,
                Direction = Vector3.forward,
                StopTime = 3.0f
            };

            path.Waypoints = new Waypoint[] { newWaypoint };
            newIndex = 0;
        }
        else
        {
            // Determine position and insert index based on selection
            if (selectedIndex >= 0 && selectedIndex < path.Waypoints.Length)
            {
                Vector3 selectedPosition = path.Waypoints[selectedIndex].Point;
                
                if (addBefore)
                {
                    insertIndex = selectedIndex;
                    
                    // If there's a waypoint before the selected one
                    if (selectedIndex > 0)
                    {
                        // Place new waypoint halfway between previous and selected
                        newPosition = Vector3.Lerp(path.Waypoints[selectedIndex - 1].Point, selectedPosition, 0.5f);
                    }
                    // If this is first waypoint but path is a loop
                    else if (path.IsLoop && path.Waypoints.Length > 1)
                    {
                        // Place new waypoint halfway between last and first
                        newPosition = Vector3.Lerp(path.Waypoints[path.Waypoints.Length - 1].Point, selectedPosition, 0.5f);
                    }
                    else
                    {
                        // No previous waypoint, use offset in opposite direction of next waypoint
                        Vector3 direction;
                        if (path.Waypoints.Length > 1)
                        {
                            // Use direction pointing away from next waypoint
                            direction = (selectedPosition - path.Waypoints[1].Point).normalized;
                        }
                        else
                        {
                            // Just use backward direction
                            direction = -Vector3.forward;
                        }
                        newPosition = selectedPosition + direction * offsetDistance;
                    }
                }
                else // Add after
                {
                    insertIndex = selectedIndex + 1;
                    
                    // If there's a waypoint after the selected one
                    if (selectedIndex < path.Waypoints.Length - 1)
                    {
                        // Place new waypoint halfway between selected and next
                        newPosition = Vector3.Lerp(selectedPosition, path.Waypoints[selectedIndex + 1].Point, 0.5f);
                    }
                    // If this is last waypoint but path is a loop
                    else if (path.IsLoop && path.Waypoints.Length > 1)
                    {
                        // Place new waypoint halfway between last and first
                        newPosition = Vector3.Lerp(selectedPosition, path.Waypoints[0].Point, 0.5f);
                    }
                    else
                    {
                        // No next waypoint, use offset in direction from previous waypoint
                        Vector3 direction;
                        if (selectedIndex > 0)
                        {
                            // Use same direction as from previous waypoint
                            direction = (selectedPosition - path.Waypoints[selectedIndex - 1].Point).normalized;
                        }
                        else
                        {
                            // Just use forward direction
                            direction = Vector3.forward;
                        }
                        newPosition = selectedPosition + direction * offsetDistance;
                    }
                }
            }
            else
            {
                // No selection, add to end
                insertIndex = path.Waypoints.Length;
                if (path.Waypoints.Length > 0)
                {
                    // Add after the last waypoint
                    Vector3 lastPosition = path.Waypoints[path.Waypoints.Length - 1].Point;
                    
                    // If there are at least 2 waypoints, continue the path direction
                    if (path.Waypoints.Length > 1)
                    {
                        Vector3 direction = (lastPosition - path.Waypoints[path.Waypoints.Length - 2].Point).normalized;
                        newPosition = lastPosition + direction * offsetDistance;
                    }
                    else
                    {
                        // Only one waypoint, use default offset
                        newPosition = lastPosition + Vector3.forward * offsetDistance;
                    }
                }
                else
                {
                    newPosition = Vector3.zero;
                }
            }

            // Create and insert the new waypoint
            Waypoint newWaypoint = new Waypoint
            {
                Point = newPosition,
                HasStop = false,
                HasDirection = false,
                Direction = Vector3.forward,
                StopTime = 3.0f
            };

            // Inert the waypoint at the calculated index
            Waypoint[] currentWaypoints = path.Waypoints;
            Waypoint[] newWaypoints = new Waypoint[currentWaypoints.Length + 1];
            
            // Copy the waypoints before insertion point
            System.Array.Copy(currentWaypoints, 0, newWaypoints, 0, insertIndex);
            
            // Insert the new waypoint
            newWaypoints[insertIndex] = newWaypoint;
            
            // Copy the remaining waypoints
            if (insertIndex < currentWaypoints.Length)
            {
                System.Array.Copy(currentWaypoints, insertIndex, newWaypoints, insertIndex + 1, 
                    currentWaypoints.Length - insertIndex);
            }
            
            path.Waypoints = newWaypoints;
            newIndex = insertIndex;
        }

        EditorUtility.SetDirty(path);
    }
    
    public void RemoveWaypoint(NpcPath path, int index)
    {
        if (index < 0 || index >= path.Waypoints.Length)
            return;
        
        // Register the operation with Undo system
        Undo.RecordObject(path, "Remove Waypoint");
            
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