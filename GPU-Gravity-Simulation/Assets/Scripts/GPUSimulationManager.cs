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
    [SerializeField] private float simulationSize;
    [SerializeField] private float smoothingRadius;
    [SerializeField] private float mass;
    [SerializeField] private float dt;
    [SerializeField] private float spiralVelocity;
    [SerializeField] private bool InitializeSpiral; 

    private int kernelComputeGravity;
    
    private Vector2[] positions;
    private Vector2[] velocities;

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
        positions = new Vector2[nParticles];
        velocities = new Vector2[nParticles];

        for (int i = 0; i < nParticles; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Random.Range(0f, 1f)) * simulationSize; 

            radius = Mathf.Pow(Random.Range(0f, 1f), 2f) * simulationSize;

            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            
            if (InitializeSpiral)
                positions[i] = new Vector2(x, y);
            else
                positions[i] = new Vector2(Random.Range(-simulationSize, simulationSize),
                    Random.Range(-simulationSize, simulationSize));

            Vector2 radialDir = new Vector2(x, y).normalized;
            Vector2 tangent = new Vector2(-radialDir.y, radialDir.x);

            float speed = Mathf.Sqrt(radius + 0.1f) * spiralVelocity;

            velocities[i] = tangent * speed;
        }

        positionsBuffer = new ComputeBuffer(nParticles, sizeof(float) * 2);
        positionsBuffer.SetData(positions);
        
        velocitiesBuffer = new ComputeBuffer(nParticles, sizeof(float) * 2);
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
        HandleControls();
    
        SetComputeShaderParameters();
        RunComputeShader();
        RenderParticles();
    
        material.SetFloat("_Size", particleRadius);
    }

    private void HandleControls()
    {
        if (Input.GetKeyDown(KeyCode.R))
            ResetSimulation();

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            ChangeParticleCount(10000);

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            ChangeParticleCount(-10000);

        if (Input.GetKeyDown(KeyCode.G))
            gravity = -gravity;

        if (Input.GetKeyDown(KeyCode.Space))
            paused = !paused;
        
        if (Input.GetKeyDown(KeyCode.RightBracket))
            spiralVelocity += 0.5f;
        if (Input.GetKeyDown(KeyCode.LeftBracket))
            spiralVelocity -= 0.5f;
        
        if (Input.GetKeyDown(KeyCode.Period))
            dt += 0.01f;
        if (Input.GetKeyDown(KeyCode.Comma))
            dt -= 0.01f;
        
        if (Input.GetKeyDown(KeyCode.Quote))
            particleRadius += 1f;
        if (Input.GetKeyDown(KeyCode.Semicolon))
            particleRadius -= 1f;
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
            simulationSize += 10f;
        if (Input.GetKeyDown(KeyCode.DownArrow))
            simulationSize -= 10f;
        
        if (Input.GetKeyDown(KeyCode.RightArrow))
            gravity += 1f;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            gravity -= 1f;

        if (Input.GetKeyDown(KeyCode.N))
            InitializeSpiral = !InitializeSpiral;
    }

    private bool paused = false;

    private void ResetSimulation()
    {
        ReleaseBuffers();
        InitializeParticles();
        InitializeRendering();
    }

    private void ChangeParticleCount(int delta)
    {
        nParticles = Mathf.Max(1000, nParticles + delta);
        ResetSimulation();
    }

    private void ReleaseBuffers()
    {
        positionsBuffer?.Release();
        velocitiesBuffer?.Release();
        argsBuffer?.Release();
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
