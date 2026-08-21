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
        // NOTE: GUIStyles must NOT be created here — GUI.skin is only valid inside OnGUI.
        // Styles are lazily built on the first OnGUI call instead (see stylesReady flag).
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

        if (!paused)
        {
            SetComputeShaderParameters();
            RunComputeShader();
        }

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

        if (Input.GetKeyDown(KeyCode.H))
            showGUI = !showGUI;
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

        if (panelTex != null) Destroy(panelTex);
        if (whiteTex != null) Destroy(whiteTex);
        if (blackTex != null) Destroy(blackTex);
        if (btnHoverTex != null) Destroy(btnHoverTex);
        if (btnActiveTex != null) Destroy(btnActiveTex);
    }

    // GUI 

    private bool showGUI = true;
    private bool stylesReady = false;

    private Texture2D panelTex;
    private Texture2D whiteTex;
    private Texture2D blackTex;
    private Texture2D btnHoverTex;
    private Texture2D btnActiveTex;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private GUIStyle toggleStyle;
    private GUIStyle actionButtonStyle;
    private GUIStyle stepButtonStyle;
    private GUIStyle hintStyle;
    private GUIStyle sectionStyle;

    private static Texture2D MakeTex(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
    
    private void InitializeGUIStyles()
    {
        Color bg = new Color(0.05f, 0.05f, 0.05f, 0.92f);

        panelTex = MakeTex(bg);
        whiteTex = MakeTex(Color.white);
        blackTex = MakeTex(Color.black);
        btnHoverTex = MakeTex(new Color(0.85f, 0.85f, 0.85f, 1f));
        btnActiveTex = MakeTex(new Color(0.6f, 0.6f, 0.6f, 1f));

        panelStyle = new GUIStyle();
        panelStyle.normal.background = panelTex;
        panelStyle.padding = new RectOffset(20, 20, 18, 18);

        headerStyle = new GUIStyle();
        headerStyle.fontSize = 20;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;
        headerStyle.margin = new RectOffset(0, 0, 0, 10);

        sectionStyle = new GUIStyle();
        sectionStyle.fontSize = 12;
        sectionStyle.fontStyle = FontStyle.Bold;
        sectionStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
        sectionStyle.margin = new RectOffset(0, 0, 12, 4);

        labelStyle = new GUIStyle();
        labelStyle.fontSize = 13;
        labelStyle.normal.textColor = Color.white;
        labelStyle.padding = new RectOffset(0, 0, 4, 0);

        valueStyle = new GUIStyle(labelStyle);
        valueStyle.alignment = TextAnchor.MiddleCenter;
        valueStyle.normal.textColor = Color.white;
        valueStyle.fontStyle = FontStyle.Bold;

        toggleStyle = new GUIStyle(GUI.skin.toggle);
        toggleStyle.fontSize = 13;
        toggleStyle.normal.textColor = Color.white;
        toggleStyle.onNormal.textColor = Color.white;
        toggleStyle.margin = new RectOffset(0, 0, 4, 4);

        actionButtonStyle = new GUIStyle();
        actionButtonStyle.fontSize = 13;
        actionButtonStyle.fontStyle = FontStyle.Bold;
        actionButtonStyle.alignment = TextAnchor.MiddleCenter;
        actionButtonStyle.normal.textColor = Color.black;
        actionButtonStyle.normal.background = whiteTex;
        actionButtonStyle.hover.textColor = Color.black;
        actionButtonStyle.hover.background = btnHoverTex;
        actionButtonStyle.active.textColor = Color.white;
        actionButtonStyle.active.background = blackTex;
        actionButtonStyle.margin = new RectOffset(0, 0, 10, 0);
        actionButtonStyle.padding = new RectOffset(0, 0, 8, 8);

        stepButtonStyle = new GUIStyle();
        stepButtonStyle.fontSize = 16;
        stepButtonStyle.fontStyle = FontStyle.Bold;
        stepButtonStyle.alignment = TextAnchor.MiddleCenter;
        stepButtonStyle.normal.textColor = Color.black;
        stepButtonStyle.normal.background = whiteTex;
        stepButtonStyle.hover.textColor = Color.black;
        stepButtonStyle.hover.background = btnHoverTex;
        stepButtonStyle.active.textColor = Color.white;
        stepButtonStyle.active.background = blackTex;
        stepButtonStyle.fixedWidth = 28;
        stepButtonStyle.fixedHeight = 24;

        hintStyle = new GUIStyle();
        hintStyle.fontSize = 11;
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.4f);
        hintStyle.margin = new RectOffset(0, 0, 14, 0);

        stylesReady = true;
    }

    private const float panelWidth = 300f;

    private void OnGUI()
    {
        if (!stylesReady)
            InitializeGUIStyles();

        GUI.Label(new Rect(16, Screen.height - 26, 300, 20),
            showGUI ? "H  —  hide panel" : "H  —  show panel", hintStyle);

        if (!showGUI)
            return;

        float x = Screen.width - panelWidth - 20;
        float y = 20;

        GUILayout.BeginArea(new Rect(x, y, panelWidth, Screen.height - 40), panelStyle);
        GUILayout.BeginVertical();

        GUILayout.Label("PARTICLE SIMULATION", headerStyle);

        GUILayout.Label("SIMULATION", sectionStyle);
        paused = GUILayout.Toggle(paused, paused ? "Paused" : "Running", toggleStyle);
        InitializeSpiral = GUILayout.Toggle(InitializeSpiral, "Spiral Initialization", toggleStyle);

        GUILayout.Label("PHYSICS", sectionStyle);
        DrawStepRow("Gravity", ref gravity, 0.1f, -1000f, 1000f);
        DrawStepRow("Mass", ref mass, 0.1f, 0.01f, 1000f);
        DrawStepRow("Smoothing Radius", ref smoothingRadius, 0.5f, 0.01f, 1000f);
        DrawStepRow("Time Step", ref dt, 0.01f, 0f, 5f);
        DrawStepRow("Spiral Velocity", ref spiralVelocity, 0.5f, 0f, 1000f);

        GUILayout.Label("WORLD", sectionStyle);
        DrawStepRow("Simulation Size", ref simulationSize, 10f, 1f, 100000f);
        DrawStepRow("Particle Radius", ref particleRadius, 1f, 0.1f, 1000f);

        int deltaParticles = DrawIntStepRow("Particle Count", nParticles, 1000);
        if (deltaParticles != 0)
            ChangeParticleCount(deltaParticles);

        // ---- Actions ----
        if (GUILayout.Button("RESET SIMULATION", actionButtonStyle))
            ResetSimulation();

        GUILayout.Label("R reset · Space pause · G flip gravity · N spiral · H toggle GUI", hintStyle);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawStepRow(string label, ref float value, float step, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(120));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("-", stepButtonStyle))
            value = Mathf.Clamp(value - step, min, max);

        GUILayout.Label(value.ToString("0.###"), valueStyle, GUILayout.Width(60));

        if (GUILayout.Button("+", stepButtonStyle))
            value = Mathf.Clamp(value + step, min, max);

        GUILayout.EndHorizontal();
    }
    
    private int DrawIntStepRow(string label, int value, int step)
    {
        int delta = 0;

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(120));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("-", stepButtonStyle))
            delta = -step;

        GUILayout.Label(value.ToString("N0"), valueStyle, GUILayout.Width(60));

        if (GUILayout.Button("+", stepButtonStyle))
            delta = step;

        GUILayout.EndHorizontal();

        return delta;
    }
}