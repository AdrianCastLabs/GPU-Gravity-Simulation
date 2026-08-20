using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationManager : MonoBehaviour
{
    [Header("Simulation Settings")]
    public GameObject particlePrefab;
    
    [Header("Simulation Parameters")]
    public int nParticles;
    public float simulationSize;
    public float gravityMultiplier;
    
    private Vector3[] positions;
    private Vector3[] velocities;
    private GameObject[] gameObjects;

    
    private void Start()
    {
        // Initialize arrays
        positions = new Vector3[nParticles];
        velocities = new Vector3[nParticles];
        gameObjects = new GameObject[nParticles];

        // Initialize particles
        for (int i = 0; i < nParticles; i++)
        {
            positions[i] = Random.insideUnitSphere * simulationSize;
            velocities[i] = Vector3.zero;
            gameObjects[i] = Instantiate(particlePrefab, positions[i], Quaternion.identity);
        }
        
    }

    private void Update()
    {
        // Simulation loop
        for (int i = 0; i < nParticles; i++)
        {
            for (int j = 0; j < nParticles; j++)
            {
                if (i == j) continue;
                
                Vector3 direction = positions[j] - positions[i];
                float distance = direction.magnitude;
                float forceMagnitude = gravityMultiplier / (distance * distance);
                velocities[i] += forceMagnitude * direction * Time.deltaTime;
            }
            
            
            positions[i] += velocities[i] * Time.deltaTime;
            gameObjects[i].transform.position = positions[i];
        }
    }
}
