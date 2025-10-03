using UnityEngine;
using UnityEngine.UI;
using Random = Unity.Mathematics.Random;

public class GlitchGenerator : MonoBehaviour
{
    [SerializeField] private int seed;
    private Random rng;

    private Transform[] cachedTransforms;
    private MeshRenderer[] cachedRenderers;
    private Image[] cachedImages;

    private void OnEnable()
    {
        rng = new Random((uint) seed);
        CacheObjects();
    }

    void CacheObjects()
    {
        cachedTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        cachedRenderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        cachedImages = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    void FixedUpdate()
    {
        var methods = new System.Action[]
        {
            RandomizePositionOfRandomObject,
            RandomizeRotationOfRandomObject,
            RandomizeScaleOfRandomObject,
            RandomizeColorOfRandomRenderer,
            RandomizeColorOfRandomImage,
            CreateRandomPrimitive
        };

        for (int i = 0; i < 3; i++)
        {
            var methodToCall = methods[rng.NextInt(0, methods.Length)];
            methodToCall();
        }
        
    }

    void RandomizePositionOfRandomObject()
    {
        var randomTransform = cachedTransforms[rng.NextInt(0, cachedTransforms.Length)];
        if (randomTransform == null)
        {
            return;
        }
        randomTransform.position += new Vector3(rng.NextFloat(-3f, 3f), rng.NextFloat(-3f, 3f), rng.NextFloat(-3f, 3f));
    }
    
    void RandomizeRotationOfRandomObject()
    {
        var randomTransform = cachedTransforms[rng.NextInt(0, cachedTransforms.Length)];
        if (randomTransform == null)
        {
            return;
        }
        randomTransform.rotation = Quaternion.Euler(rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f));
    }
    
    void RandomizeScaleOfRandomObject()
    {
        var randomTransform = cachedTransforms[rng.NextInt(0, cachedTransforms.Length)];
        if (randomTransform == null)
        {
            return;
        }
        randomTransform.localScale *= rng.NextFloat(0.5f, 2f);
    }

    void RandomizeColorOfRandomRenderer()
    {
        var randomRenderer = cachedRenderers[rng.NextInt(0, cachedRenderers.Length)];
        if (randomRenderer == null)
        {
            return;
        }
        if (randomRenderer.material.HasProperty("_Color"))
        {
            randomRenderer.material.color = new Color(rng.NextFloat(0f, 1f), rng.NextFloat(0f, 1f), rng.NextFloat(0f, 1f));
        }
    }

    void RandomizeColorOfRandomImage()
    {
        var randomImage = cachedImages[rng.NextInt(0, cachedImages.Length)];
        if (randomImage == null)
        {
            return;
        }
        randomImage.color = new Color(rng.NextFloat(0f, 1f), rng.NextFloat(0f, 1f), rng.NextFloat(0f, 1f), randomImage.color.a);
    }

    void CreateRandomPrimitive()
    {
        if(rng.NextFloat(0f, 1f) < 0.5f) return;
        
        var primitiveTypes = new PrimitiveType[]
        {
            PrimitiveType.Cube,
            PrimitiveType.Sphere,
            PrimitiveType.Capsule,
            PrimitiveType.Cylinder,
            PrimitiveType.Plane,
            PrimitiveType.Quad
        };
        
        var randomType = primitiveTypes[rng.NextInt(0, primitiveTypes.Length)];
        var primitive = GameObject.CreatePrimitive(randomType);
        primitive.transform.position = new Vector3(rng.NextFloat(-10f, 10f), rng.NextFloat(-10f, 10f), rng.NextFloat(-10f, 10f));
        primitive.transform.localScale = Vector3.one * rng.NextFloat(0.5f, 2f);
        primitive.transform.rotation = Quaternion.Euler(rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f));
        var renderer = primitive.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material.HasProperty("_Color") && rng.NextFloat(0f, 1f) > 0.8f)
        {
            renderer.material.color = new Color(rng.NextFloat(0f, 1f), rng.NextFloat(0f, 1f), rng.NextFloat(0f, 1f));
        }
    }
}