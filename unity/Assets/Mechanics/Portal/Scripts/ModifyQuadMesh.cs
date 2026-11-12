using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ModifyQuadMesh : MonoBehaviour {

    public float x = 0f;
    public float y = 0f;
    public float z = 0f;
    void Start () {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh; // Pega a cópia da mesh
        Vector3[] vertices = mesh.vertices;
        vertices[3] += new Vector3(x, y, z);
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}