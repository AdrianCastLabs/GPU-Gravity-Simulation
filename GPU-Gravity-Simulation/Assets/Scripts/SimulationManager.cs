using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationManager : MonoBehaviour
{
    // Simulation settings
    public GameObject particlePrefab;
    
    // Simulation parameters
    public int nParticles;
    public float simulationSize;
    public float gravityMultiplier;
    
    private Vector3[] positions;
    private GameObject[] gameObjects;

    
    private void Start()
    {
        // Initialize arrays
        positions = new Vector3[nParticles];
        gameObjects = new GameObject[nParticles];

        // Initialize particles
        for (int i = 0; i < nParticles; i++)
        {
            positions[i] = Random.insideUnitSphere * simulationSize;
            gameObjects[i] = Instantiate(particlePrefab, positions[i], Quaternion.identity);
        }
        
    }

    private void Update()
    {
        // Simulation loop
        for (int i = 0; i < nParticles; i++)
        {
            positions[i].y -= 1f * Time.deltaTime * gravityMultiplier;
            gameObjects[i].transform.position = positions[i];
        }
    }
}
