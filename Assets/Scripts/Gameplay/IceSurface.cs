using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Put on a ground collider: the hero accelerates and stops slowly here.</summary>
    public class IceSurface : MonoBehaviour
    {
        [Range(0.02f, 1f)] public float frictionScale = 0.15f;
    }
}
