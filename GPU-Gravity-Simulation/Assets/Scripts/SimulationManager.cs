using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationManager : MonoBehaviour
{
    public GameObject particlePrefab;

    public float simulationSize;
    
    private void Start()
    {
        GameObject newParticle = Instantiate(particlePrefab, Random.insideUnitCircle * simulationSize, Quaternion.identity);
    }
}
