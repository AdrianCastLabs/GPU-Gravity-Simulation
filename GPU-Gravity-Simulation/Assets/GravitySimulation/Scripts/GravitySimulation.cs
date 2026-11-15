using System.ComponentModel;
using UnityEngine;

public class GravitySimulation : MonoBehaviour
{
    [SerializeField] private GameObject particlePrefab;

    [SerializeField] private float Gravity = 1;
    [SerializeField] private float Velocity;

    private void Update()
    {
        Velocity += Gravity * Time.deltaTime;

        Vector2 pos = particlePrefab.transform.position;
        particlePrefab.transform.position = new Vector2(pos.x, pos.y - Velocity);
    }
}