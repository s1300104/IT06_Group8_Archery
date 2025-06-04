using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class ZigzagGhostMovement : MonoBehaviour
{
    private int reverse;
    private int zigzag;
    private float moveX = 0.2f;
    private float moveZ = 0.2f;
 
    void Update()
    {
        transform.Translate(new Vector3(moveX, 0, moveZ));
        
        reverse++;
        if (reverse == 100)
        {
           reverse = 0;
           transform.Rotate(new Vector3(0, Random.Range(0, 180), 0));
        }
 
        zigzag++;
        if (zigzag == 10)
        {
            zigzag = 0;
            moveX *= -1;
        }
    }
}