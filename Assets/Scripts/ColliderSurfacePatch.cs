using Oculus.Interaction.Surfaces;
using UnityEngine;

public class ColliderSurfacePatch : MonoBehaviour, ISurfacePatch
{
    public ColliderSurface surface;

    public ISurface BackingSurface => surface;

    public Transform Transform => surface.Transform;

    public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0)
    {
        return surface.ClosestSurfacePoint(point, out hit, maxDistance);
    }

    public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0)
    {
        return surface.Raycast(ray, out hit, maxDistance);
    }
}