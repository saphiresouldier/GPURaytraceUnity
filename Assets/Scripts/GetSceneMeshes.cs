using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GetSceneMeshes : Singleton<GetSceneMeshes> {

    private MeshFilter[] _meshRenderers; //TODO: Renderer?
    private List<Triangle> _meshTriangles;

	// Use this for initialization
	public override void Awake () {
        base.Awake();

        _meshTriangles = GetRenderersCurrentScene();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public MeshFilter[] GetSceneRenderers()
    {
        return _meshRenderers;
    }

    public List<Triangle> GetSceneTriangles()
    {
        return _meshTriangles;
    }

    private List<Triangle> GetRenderersCurrentScene()
    {
        _meshRenderers = FindObjectsOfType<MeshFilter>();

        List<Triangle> tris = new List<Triangle>();

        foreach(MeshFilter r in _meshRenderers)
        {
            Debug.Log("Found object with Meshfilter: " + r.gameObject.name);
            Mesh m = r.mesh;

            int[] triangles = m.triangles;
            Vector3[] vertices = m.vertices;
            //Vector2[] uvs = m.uv; //TODO: Necessary for texture support later

            Debug.Log("It consists of " + vertices.Length + " vertices and " + triangles.Length + " triangles!");
            for(int i = 0; i < triangles.Length; i += 3)
            {
                Triangle t = new Triangle();
                t.v1 = r.transform.localToWorldMatrix.MultiplyPoint3x4(vertices[triangles[i]]);
                t.v2 = r.transform.localToWorldMatrix.MultiplyPoint3x4(vertices[triangles[i + 1]]);
                t.v3 = r.transform.localToWorldMatrix.MultiplyPoint3x4(vertices[triangles[i + 2]]);
                t.normal = ComputeTriangleNormal(t.v1, t.v2, t.v3);
                t.albedo = new Vector3(0.4f, 0.4f, 0.4f);
                t.specular = new Vector3(0.8f, 0.8f, 0.8f);

                tris.Add(t);
            }

        }

        return tris;
    }

    private Vector3 ComputeTriangleNormal(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 v3v1 = v3 - v1;
        Vector3 v2v1 = v2 - v1;
        return Vector3.Cross(v3v1, v2v1).normalized;
    }
}
