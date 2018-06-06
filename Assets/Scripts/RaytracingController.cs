﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Sphere
{
    public Vector3 position;
    public float radius;
    public Vector3 albedo;
    public Vector3 specular;
};

public struct Triangle
{
    public Vector3 v1, v2, v3;
    public Vector3 normal; //flat shading will do for now
    public Vector3 albedo;
    public Vector3 specular;
};

public class RaytracingController : MonoBehaviour {

    public ComputeShader RayTraceShader;
    public Camera Camera;
    public Texture SkyboxTex;
    public Light DirectionalLight;

    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;

    [SerializeField]
    private float _skyboxMultiplicator = 1.0f;
    private RenderTexture _targetTex;
    private uint _currentSample = 0;
    private Material _addMaterial;

    private ComputeBuffer _sphereBuffer;
    private ComputeBuffer _triangleBuffer;

    public float SkyboxMultiplicator
    {
        get { return _skyboxMultiplicator; }
        set {
            _skyboxMultiplicator = value;
            RestartSampling();
        }
    }

    private void Awake()
    {
        if (Camera == null) Camera = Camera.main;
    }

    private void OnEnable()
    {
        _currentSample = 0;
        SetupSphereScene();
        SetupTriangleScene();
    }

    private void Update()
    {
        DetectTransformChanged(Camera.transform);
        DetectTransformChanged(DirectionalLight.transform);
    }

    private void DetectTransformChanged(Transform t)
    {
        if(t.hasChanged)
        {
            RestartSampling();
            t.hasChanged = false;
        }
    }

    private void SetupSphereScene()
    {
        List<Sphere> spheres = new List<Sphere>();
        // Add a number of random spheres
        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();

            // Radius and radius
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            // Reject spheres that are intersecting others
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                {
                    goto SkipSphere;
                }
                    
            }
            // Albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            
            // Add the sphere to the list
            spheres.Add(sphere);

        SkipSphere:
            continue;
        }
        // Assign to compute buffer, 40 is byte size of sphere struct in memory
        _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _sphereBuffer.SetData(spheres);
    }

    private void SetupTriangleScene()
    {
        List<Triangle> tris = GetSceneTriangles(false);
        
        // Assign to compute buffer, 72 is byte size of sphere struct in memory
        _triangleBuffer = new ComputeBuffer(tris.Count, 72);
        _triangleBuffer.SetData(tris);
    }

    private List<Triangle> GetSceneTriangles(bool generated = false)
    {
        List<Triangle> triangles = new List<Triangle>();

        if(generated)
        {
            // Add a number of random spheres
            for (int i = 0; i < SpheresMax; i++)
            {
                Triangle tri = new Triangle();

                float radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
                Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
                Vector3 position = new Vector3(randomPos.x, radius, randomPos.y);
                tri.v1 = Random.onUnitSphere * radius + position;
                tri.v2 = Random.onUnitSphere * radius + position;
                tri.v3 = Random.onUnitSphere * radius + position;
                tri.normal = ComputeTriangleNormal(tri.v1, tri.v2, tri.v3);

                // Albedo and specular color
                Color color = Random.ColorHSV();
                bool metal = Random.value < 0.5f;
                tri.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
                tri.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;

                // Add the sphere to the list
                triangles.Add(tri);
            }
        }
        else //Get actual Unity scene triangles
        {
            triangles = GetSceneMeshes.Instance.GetSceneTriangles();
        }

        return triangles;
    }

    private Vector3 ComputeTriangleNormal(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 v3v1 = v3 - v1;
        Vector3 v2v1 = v2 - v1;
        return Vector3.Cross(v2v1, v3v1).normalized;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void SetShaderParameters()
    {
        RayTraceShader.SetMatrix("_CameraToWorld", Camera.cameraToWorldMatrix);
        RayTraceShader.SetMatrix("_CameraInverseProjection", Camera.projectionMatrix.inverse);
        RayTraceShader.SetTexture(0, "_SkyboxTex", SkyboxTex);
        RayTraceShader.SetFloat("_SkyboxTexFactor", _skyboxMultiplicator);
        RayTraceShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        Vector3 l = DirectionalLight.transform.forward;
        RayTraceShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        RayTraceShader.SetBuffer(0, "_Spheres", _sphereBuffer);
        RayTraceShader.SetBuffer(0, "_Triangles", _triangleBuffer);
    }

    private void Render(RenderTexture dest)
    {
        //Do we have a render target?
        InitRenderTexture();

        //set target, dispatch computeshader
        RayTraceShader.SetTexture(0, "Result", _targetTex);
        int threadGroupAmountX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupAmountY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTraceShader.Dispatch(0, threadGroupAmountX, threadGroupAmountY, 1);

        //show resulting texture
        // Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_targetTex, dest, _addMaterial);
        _currentSample++;
        //Graphics.Blit(_targetTex, dest);
    }

    private void InitRenderTexture()
    {
        if(_targetTex == null || _targetTex.width != Screen.width || _targetTex.height != Screen.height)
        {
            //Restart with sample 0
            _currentSample = 0;

            //release old render texture
            if (_targetTex != null) _targetTex.Release();

            //Create render texture for raytracing
            _targetTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _targetTex.enableRandomWrite = true;
            _targetTex.Create();
        }
    }

    private void RestartSampling()
    {
        _currentSample = 0;
    }
}
