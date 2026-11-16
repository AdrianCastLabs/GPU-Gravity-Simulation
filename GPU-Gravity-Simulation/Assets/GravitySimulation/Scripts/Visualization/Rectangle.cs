using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Rectangle : MonoBehaviour
{
    [Header("Shape")]
    public Vector2 size = new Vector2(2f, 1f);
    [Range(0f, 0.5f)]
    public float cornerRadius = 0.15f;
    [Range(1, 16)]
    public int cornerSegments = 6;
    
    [Header("Appearance")]
    public Color color = Color.cyan;

    private Mesh mesh;

    private void OnEnable() => Generate();
    private void OnValidate() => Generate();

    private void Generate()
    {
        if (size.x <= 0 || size.y <= 0)
            return;

        if (mesh == null)
        {
            mesh = new Mesh { name = "Rounded Rectangle" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        mesh.Clear();

        float radius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(size.x, size.y) * 0.5f);
        int totalCornerVerts = cornerSegments + 1;
        Vector3[] vertices = new Vector3[1 + 4 * totalCornerVerts];
        vertices[0] = Vector3.zero;

        Vector2 half = size * 0.5f;
        Vector2[] centers =
        {
            new Vector2(half.x - radius, half.y - radius),
            new Vector2(-half.x + radius, half.y - radius),
            new Vector2(-half.x + radius, -half.y + radius),
            new Vector2(half.x - radius, -half.y + radius)
        };

        int index = 1;
        for (int c = 0; c < 4; c++)
        {
            float startAngle = c * 90f;
            for (int i = 0; i <= cornerSegments; i++)
            {
                float angle = Mathf.Deg2Rad * (startAngle + 90f * i / cornerSegments);
                vertices[index++] = new Vector3(
                    centers[c].x + Mathf.Cos(angle) * radius,
                    centers[c].y + Mathf.Sin(angle) * radius,
                    0f
                );
            }
        }

        int count = 4 * totalCornerVerts;
        int[] triangles = new int[count * 3];
        int t = 0;

        for (int i = 1; i < count; i++)
        {
            triangles[t++] = 0;
            triangles[t++] = i;
            triangles[t++] = i + 1;
        }

        triangles[t++] = 0;
        triangles[t++] = count;
        triangles[t] = 1;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            mr.sharedMaterial = new Material(shader);
        }

        mr.sharedMaterial.color = color;
    }
}