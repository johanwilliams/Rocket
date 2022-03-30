using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoRotation2D : MonoBehaviour
{
    // Update is called once per frame
    void LateUpdate()
    {
        transform.up = Vector2.up;
    }
}
