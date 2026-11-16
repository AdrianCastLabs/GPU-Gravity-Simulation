using System;
using UnityEngine;

public class GravitySimulation : MonoBehaviour
{
    [SerializeField] private Transform particleTransform;
    [SerializeField] private Transform floorTransform;

    [SerializeField] private float gravity = 1 ;
    [SerializeField] private float damping = 0.8f;
    [SerializeField] private float floorOffsetY = 1;

    private float velocityY;

    private void Update()
    {
        velocityY -= gravity * Time.deltaTime;

        Vector3 pos = particleTransform.position;
        pos.y += velocityY * Time.deltaTime;
        
        float floorY = floorTransform.position.y + floorOffsetY;
        if (pos.y <= floorY)
        {
            pos.y = floorY;

            if (velocityY < 0)
            {
                velocityY = -velocityY * damping;
            }
        }

        particleTransform.position = pos;
    }
}