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

        foreach (MeshFilter r in _meshRenderers)
        {
            Debug.Log("Found object with Meshfilter: " + r.gameObject.name);
            Mesh m = r.mesh;

            int[] triangles = m.triangles;
            Vector3[] vertices = m.vertices;
            //Vector2[] uvs = m.uv; //TODO: Necessary for texture support later

            Debug.Log("It consists of " + vertices.Length + " vertices and " + triangles.Length + " triangle indices, which amounts to " + triangles.Length / 3.0f + " triangles!");
            for(int i = 0; i < triangles.Length; i += 3)
            {
                Triangle t = new Triangle();
                t.v1 = r.transform.localToWorldMatrix.MultiplyPoint3x4(vertices[triangles[i]]);
                t.v2 = r.transform.localToWorldMatrix.MultiplyPoint3x4(vertices[triangles[i + 1]]);
                t.v3 = r.transform.localToWorldMatrix.MultiplyPoint3x4(vertices[triangles[i + 2]]);
                t.normal = ComputeTriangleNormal(t.v1, t.v2, t.v3);

                t.material = new RaytraceMaterial();  //TODO: Reuse materials

                Material mat = r.GetComponent<Renderer>().sharedMaterial; ;
                if (mat == null) Debug.Log("Error getting material!");
                t.material.albedo = new Vector3(mat.GetColor("_Albedo").r, mat.GetColor("_Albedo").g, mat.GetColor("_Albedo").b);
                t.material.specular = new Vector3(mat.GetColor("_Specular").r, mat.GetColor("_Specular").g, mat.GetColor("_Specular").b);
                t.material.smoothness = mat.GetFloat("_Smoothness");
                t.material.emission = mat.GetFloat("_Emission");

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
