using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class CPUSimulationManager : MonoBehaviour
{
    [Header("Simulation Settings")]
    public GameObject particlePrefab;
    
    [Header("Simulation Parameters")]
    public int nParticles;
    public float simulationSize;
    public float gravityMultiplier;
    public float deltaTime;
    public float minMass;
    public float maxMass;
    public float initialVelocity;
    public float particleSize;
    
    private Vector3[] positions;
    private Vector3[] velocities;
    private float[] masses;
    private GameObject[] gameObjects;

    
    private void Start()
    {
        // Initialize arrays
        positions = new Vector3[nParticles];
        velocities = new Vector3[nParticles];
        masses = new float[nParticles];
        gameObjects = new GameObject[nParticles];

        // Initialize particles
        for (int i = 0; i < nParticles; i++)
        {
            positions[i] = Random.insideUnitCircle * simulationSize;
            velocities[i] = Random.insideUnitCircle * initialVelocity;
            masses[i] = Random.Range(minMass, maxMass);
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
                
                if (distance < 0.1f + masses[i]) continue;
                
                Vector3 acceleration = direction.normalized * gravityMultiplier * masses[j] / (distance * distance);
                velocities[i] += acceleration * deltaTime;
            }
            
        }
        
        for (int i = 0; i < nParticles; i++)
        {
            positions[i] += velocities[i] * deltaTime;
            positions[i].z = 0f;
            gameObjects[i].transform.position = positions[i];
            gameObjects[i].transform.localScale = new Vector3(particleSize + masses[i], particleSize + masses[i], particleSize + masses[i]);

        }
    }
}
