using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

/// <summary>
/// 玩家视野遮罩（Fog of War / Visibility Mesh）| 墙体伪3D 核心脚本
/// 功能：
/// 1. 根据墙体（Wall Tilemap 的 CompositeCollider2D）生成被遮挡"阴影"
/// 3. 支持面向剔除、动态颜色同步、相机自适应扫描半径等高级特性
/// </summary>
public class PlayerVisibilityMesh : MonoBehaviour
{

    [SerializeField] private string wallTilemapName = "Wall";           // 墙体 Tilemap GameObject 名字
    //[SerializeField] private LayerMask wallLayerMask = -1;             // 墙体所处 Layer（备用，当前未使用 Raycast）
    //[SerializeField] private float rangeRadius = 8f;                   // 玩家最大可视距离
    [SerializeField] private float rayLength = 8f;                   // 射线延伸的最远距离
    [SerializeField] private bool useWallColor = false;              // 用wall的颜色还是材质
    [SerializeField] private string sortingLayerName = "Player";
    [SerializeField] private int sortingOrder = 20;                    // 遮罩渲染顺序
    [SerializeField] private float screenMargin = 5f;                  // 相机边界额外扩展距离，防止边缘闪烁

    [SerializeField] private bool useFacingCull = true;                // 是否只处理面向玩家的墙体边缘（背对玩家的墙不生成遮罩）

    // ==================== 【运行时引用】 ====================

    private CompositeCollider2D compositeCollider;       // 墙体复合碰撞体（用于取边缘路径）
    private Tilemap wallTilemap;
    private TilemapRenderer wallTileRenderer;

    private MeshFilter meshFilter;                       // 墙体遮罩 Mesh
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    // ==================== 【生命周期】 ====================

    void Awake()
    {
        //playerCollider = GetComponent<CircleCollider2D>();

        // 自动查找墙体 Tilemap（必须有 CompositeCollider2D）
        var wallObj = GameObject.Find(wallTilemapName);
        if (wallObj)
        {
            compositeCollider = wallObj.GetComponent<CompositeCollider2D>();
            wallTilemap = wallObj.GetComponent<Tilemap>();
            wallTileRenderer = wallObj.GetComponent<TilemapRenderer>();
        }


        var sr = GetComponentInChildren<SpriteRenderer>();

        // ------------------- 创建墙体遮罩物体 -------------------
        var wallGo = new GameObject("VisibilityMesh");
        wallGo.transform.SetParent(null);
        meshFilter = wallGo.AddComponent<MeshFilter>();
        meshRenderer = wallGo.AddComponent<MeshRenderer>();

        meshRenderer.sortingLayerID = ResolveSortingLayerId(sr);
        meshRenderer.sortingOrder = sortingOrder;

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        meshFilter.sharedMesh = mesh;

    }

    private int ResolveSortingLayerId(SpriteRenderer referenceRenderer)
    {
        if (referenceRenderer != null)
        {
            return referenceRenderer.sortingLayerID;
        }

        if (!string.IsNullOrWhiteSpace(sortingLayerName))
        {
            var layers = SortingLayer.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == sortingLayerName)
                {
                    return layers[i].id;
                }
            }

            Debug.LogWarning($"[PlayerVisibilityMesh] 未找到 Sorting Layer '{sortingLayerName}'，已回退到 Default 层");
        }

        return SortingLayer.NameToID("Default");
    }

    void OnDestroy()
    {
        // 彻底销毁运行时创建的所有资源，防止泄露
        if (mesh != null) Destroy(mesh);
        if (meshRenderer != null && meshRenderer.material != null) Destroy(meshRenderer.material);
        if (meshFilter != null && meshFilter.gameObject != null) Destroy(meshFilter.gameObject);

    }

    void LateUpdate()
    {
        if (compositeCollider == null) return;

        var center = (Vector2)transform.position;

        // 动态同步墙体/地面颜色变化（如白天黑夜切换）
        UpdateFillColorFromWall();

        // 计算本次需要扫描的半径（相机自适应 or 手动）
        float r;
        // if (useCameraRadius && Camera.main != null)
        // {
        var halfH = Camera.main.orthographicSize;
        var halfW = halfH * Camera.main.aspect;
        r = Mathf.Sqrt(halfW * halfW + halfH * halfH) + screenMargin;
        // }
        // else
        // {
        //     r = scanRadius;
        // }

        BuildPerEdgeMesh(center, r);
    }

    // ==================== 【颜色同步】 ====================

    void UpdateFillColorFromWall()
    {
        var shader = Shader.Find("Sprites/Default");
        var wallMat = new Material(shader);
        // 颜色同步：使用 Tilemap 的颜色
        if (useWallColor)
        {
            // Color wallCol = wallTileRenderer != null && wallTileRenderer.material != null
            //     ? wallTileRenderer.material.color
            //     : (wallTilemap ? wallTilemap.color : Color.gray);
            Color wallCol = wallTilemap ? wallTilemap.color : Color.gray;
            // fillColor = new Color(wallCol.r, wallCol.g, wallCol.b, 1f);
            wallMat.color = wallCol;
            meshRenderer.material = wallMat;
        }
        else // 使用它TileRender材质
        {
            meshRenderer.material = wallTileRenderer ? wallTileRenderer.material : wallMat;
        }
    }



    // ==================== 【核心：每帧重建 Mesh】 ====================

    /// <summary>
    /// 按墙体和地面边缘逐条生成遮罩与地面延伸 Mesh
    /// </summary>
    void BuildPerEdgeMesh(Vector2 center, float radius)
    {
        var wallVerts = new List<Vector3>();
        var wallColors = new List<Color>();
        var wallTris = new List<int>();

        // ------------------- 处理墙体遮罩 -------------------
        var count = compositeCollider != null ? compositeCollider.pathCount : 0;
        for (int i = 0; i < count; i++)
        {
            var n = compositeCollider.GetPathPointCount(i);
            if (n < 2) continue;

            var arr = new Vector2[n];
            compositeCollider.GetPath(i, arr);

            for (int k = 0; k < n; k++)
            {
                var v1 = (Vector2)compositeCollider.transform.TransformPoint(arr[k]);
                var v2 = (Vector2)compositeCollider.transform.TransformPoint(arr[(k + 1) % n]);

                if (!SegmentInOrIntersectCamera(v1, v2)) continue;

                //var a1 = Mathf.Atan2(v1.y - center.y, v1.x - center.x);
                //var a2 = Mathf.Atan2(v2.y - center.y, v2.x - center.x);

                // 【面向剔除】只处理面向玩家的墙面（背对的不生成遮罩）
                if (useFacingCull)
                {
                    var mid = (v1 + v2) * 0.5f;
                    var toP = center - mid;
                    var dir = (v2 - v1).normalized;
                    var nrm = new Vector2(-dir.y, dir.x); // 边缘法线（向左）
                    if (Vector2.Dot(nrm, toP) <= 0f) continue;
                }

                var far = GetFarLength();

                var e1 = center + (v1 - center).normalized * far;
                var e2 = center + (v2 - center).normalized * far;


                // 构造本条边缘的多边形：v1 → v2 → (arc or e2→e1) → 返回
                var poly = new List<Vector3> { v1, v2 };
                poly.Add(e2);
                poly.Add(e1);

                var baseIndex = wallVerts.Count;
                for (int t = 0; t < poly.Count; t++)
                {
                    wallVerts.Add(poly[t]);
                    //wallColors.Add(fillColor);
                    wallColors.Add(Color.white);
                }

                // 扇形三角化（以第一个点为原点）
                for (int t = 1; t < poly.Count - 1; t++)
                {
                    wallTris.Add(baseIndex);
                    wallTris.Add(baseIndex + t);
                    wallTris.Add(baseIndex + t + 1);
                }
            }
        }


        // ------------------- 提交墙体遮罩 Mesh -------------------
        if (mesh != null)
        {
            mesh.Clear();
            mesh.vertices = wallVerts.ToArray();
            mesh.colors = wallColors.ToArray();
            mesh.triangles = wallTris.ToArray();
            mesh.RecalculateBounds();
            meshFilter.transform.position = Vector3.zero;
        }


    }

    // ==================== 【辅助函数】 ====================

    float GetFarLength()
    {
        if (Camera.main == null) return rayLength;
        var h = Camera.main.orthographicSize;
        var w = h * Camera.main.aspect;
        var d = Mathf.Sqrt(w * w + h * h) + screenMargin;
        return Mathf.Max(d, rayLength);
    }

    bool SegmentInOrIntersectCamera(Vector2 a, Vector2 b)
    {
        if (Camera.main == null) return true;
        var cam = Camera.main;
        var halfH = cam.orthographicSize;
        var halfW = halfH * cam.aspect;
        var c = (Vector2)cam.transform.position;
        var min = new Vector2(c.x - halfW - screenMargin, c.y - halfH - screenMargin);
        var max = new Vector2(c.x + halfW + screenMargin, c.y + halfH + screenMargin);
        if (PointInRect(a, min, max) || PointInRect(b, min, max)) return true;
        var rBL = new Vector2(min.x, min.y);
        var rBR = new Vector2(max.x, min.y);
        var rTR = new Vector2(max.x, max.y);
        var rTL = new Vector2(min.x, max.y);
        if (SegmentsIntersect(a, b, rBL, rBR)) return true;
        if (SegmentsIntersect(a, b, rBR, rTR)) return true;
        if (SegmentsIntersect(a, b, rTR, rTL)) return true;
        if (SegmentsIntersect(a, b, rTL, rBL)) return true;
        return false;
    }

    bool PointInRect(Vector2 p, Vector2 min, Vector2 max)
    {
        return p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
    }

    bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        if (Mathf.Max(p1.x, p2.x) < Mathf.Min(q1.x, q2.x)) return false;
        if (Mathf.Max(q1.x, q2.x) < Mathf.Min(p1.x, p2.x)) return false;
        if (Mathf.Max(p1.y, p2.y) < Mathf.Min(q1.y, q2.y)) return false;
        if (Mathf.Max(q1.y, q2.y) < Mathf.Min(p1.y, p2.y)) return false;
        float o1 = Cross(p1, p2, q1);
        float o2 = Cross(p1, p2, q2);
        float o3 = Cross(q1, q2, p1);
        float o4 = Cross(q1, q2, p2);
        return o1 * o2 <= 0f && o3 * o4 <= 0f;
    }

    float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    /// <summary>
    /// 点是否在多边形内（射线法）
    /// </summary>
    bool PointInPolygon(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;
        int j = poly.Count - 1;
        for (int i = 0; i < poly.Count; i++)
        {
            var pi = poly[i];
            var pj = poly[j];
            bool intersect = ((pi.y > p.y) != (pj.y > p.y)) &&
                             (p.x < (pj.x - pi.x) * (p.y - pi.y) / ((pj.y - pi.y) + Mathf.Epsilon) + pi.x);
            if (intersect) inside = !inside;
            j = i;
        }
        return inside;
    }

    /// <summary>
    /// 点到线段的最短距离（用于地面边缘排序）
    /// </summary>
    float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var ap = p - a;
        float t = Vector2.Dot(ap, ab) / (ab.sqrMagnitude + Mathf.Epsilon);
        t = Mathf.Clamp01(t);
        var closest = a + ab * t;
        return Vector2.Distance(p, closest);
    }
}

