﻿using UnityEngine;

public class Obstacle : MoveLeft
{
    private float leftBound = -15;

    void Update()
    {
        Move();
        DestroyIfOutofBounds();
    }

    // Destroys the game object if it goes beyond the left border
    private void DestroyIfOutofBounds()
    {
        if (transform.position.x < leftBound)
        {
            Destroy(gameObject);
        }
    }
}
