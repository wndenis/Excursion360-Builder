﻿using UnityEngine;

/**
 * @brief Represents connection of origin state to destination state
 * 
 *      ___                         ___
 *     /   \ <- orientation   ---> /   \
 *     \__\/                       \/__/
 * origin ^------------------------^ destination
 */

 
public class Connection : StateItem
{

    /// <summary>
    /// Origin of this connection
    /// </summary>
    public State Origin
    { 
        get
        {
            if (_origin == null)
                _origin = GetComponent<State>();

            return _origin;
        } 
    }

    [HideInInspector]
    public int colorScheme = 0;

    /**
     * @brief Linked connection
     */
    public State Destination;

    private State _origin; /// Cache
}
