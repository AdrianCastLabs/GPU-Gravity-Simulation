using UnityEngine;

public class GPUSimulationManager : MonoBehaviour
{
    [Header("ComputeShader")]
    [SerializeField] private ComputeShader computeShader;
    
    [Header("Rendering Settings")]
    [SerializeField] private Mesh mesh;

    [SerializeField] private Mesh pointMesh;
    [SerializeField] private Material material;
    
    [Header("Simulation Settings")]
    [SerializeField] private float gravity;
    [SerializeField] private int nParticles;
    [SerializeField] private float particleRadius;
    [SerializeField] private Vector3 simulationSize;
    [SerializeField] private float smoothingRadius;
    [SerializeField] private float mass;
    [SerializeField] private float dt;

    private int kernelComputeGravity;
    
    private Vector3[] positions;
    private Vector3[] velocities;

    private ComputeBuffer positionsBuffer;
    private ComputeBuffer velocitiesBuffer;
    private ComputeBuffer argsBuffer;
    
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds bounds;
    
    private void Start()
    {
        InitializeComputeShader();
        InitializeParticles();
        InitializeRendering();
    }
    
    private void InitializeComputeShader()
    {
        kernelComputeGravity = computeShader.FindKernel("ComputeGravity");
       
    }
    
    private void InitializeParticles()
    {
        positions = new Vector3[nParticles];
        velocities = new Vector3[nParticles];

        for (int i = 0; i < nParticles; i++)
        {
            positions[i] = new Vector3(
                Random.Range(-simulationSize.x, simulationSize.x),
                Random.Range(-simulationSize.y, simulationSize.y),
                0.0f
            );
            
            velocities[i] = new Vector3(
                Random.Range(-1.0f, 1.0f),
                Random.Range(-1.0f, 1.0f),
                0.0f
            );
           
        }

        positionsBuffer = new ComputeBuffer(nParticles, sizeof(float) * 3);
        positionsBuffer.SetData(positions);
        
        velocitiesBuffer = new ComputeBuffer(nParticles, sizeof(float) * 3);
        velocitiesBuffer.SetData(velocities);
        
        computeShader.SetBuffer(kernelComputeGravity, "positions",  positionsBuffer);
        computeShader.SetBuffer(kernelComputeGravity, "velocities", velocitiesBuffer);
        
        SetComputeShaderParameters();
    }
    
    private void SetComputeShaderParameters()
    {
        computeShader.SetFloat("gravity", gravity);
        computeShader.SetInt("nParticles", nParticles);
        computeShader.SetFloat("smoothingRadius", smoothingRadius);
        computeShader.SetFloat("mass", mass);
        computeShader.SetFloat("dt", dt);
        computeShader.SetVector("simulationSize", simulationSize);
    }
    
    private void InitializeRendering()
    {
        // setup indirect rendering
        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)nParticles;
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        
        // setup material
        material.SetBuffer("positions", positionsBuffer);
        material.SetBuffer("velocities", velocitiesBuffer);
        material.SetFloat("_Radius", particleRadius);
        
        // set bounds for culling
        bounds = new Bounds(
            Vector3.zero,
            Vector3.one * 100000.0f
        );
    }
    
    private void Update()
    {
        SetComputeShaderParameters();
        RunComputeShader();
        RenderParticles();
        
        material.SetFloat("_Size", particleRadius);
        
    }
    
    private void RunComputeShader()
    {
        int threadGroups = Mathf.CeilToInt(nParticles / 64f);
        
        computeShader.Dispatch(kernelComputeGravity, threadGroups, 1, 1);
    }
    
    private void RenderParticles()
    {
        Graphics.DrawMeshInstancedIndirect(
            mesh,
            0,
            material,
            bounds,
            argsBuffer
        );
    }

    private void OnDestroy()
    {
        positionsBuffer?.Release();
        velocitiesBuffer?.Release();
        argsBuffer?.Release();
    }

}
