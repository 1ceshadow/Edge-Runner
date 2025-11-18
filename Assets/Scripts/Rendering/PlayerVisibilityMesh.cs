using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

/// <summary>
/// 玩家视野遮罩（Fog of War / Visibility Mesh）核心脚本
/// 功能：
/// 1. 根据墙体（Wall Tilemap 的 CompositeCollider2D）生成被遮挡的黑暗区域
/// 2. 根据地面（Ground Tilemap）向可见区域外延伸地面颜色，形成“可见范围外仍有地面”的效果
/// 3. 支持圆形边界、面向剔除、动态颜色同步、相机自适应扫描半径等高级特性
/// </summary>
public class PlayerVisibilityMesh : MonoBehaviour
{

    [SerializeField] private string wallTilemapName = "Wall";           // 墙体 Tilemap GameObject 名字
    //[SerializeField] private LayerMask wallLayerMask = -1;             // 墙体所处 Layer（备用，当前未使用 Raycast）
    [SerializeField] private float rangeRadius = 8f;                   // 玩家最大可视距离（圆形边界时使用）
    [SerializeField] private float rayLength = 100f;                   // 射线延伸的最远距离（非圆形边界时使用）
    [SerializeField] private Color fillColor = Color.white;            // 遮罩填充颜色（会自动同步墙体颜色）
    [SerializeField] private int sortingOrder = 10;                    // 遮罩渲染顺序
    [SerializeField] private float arcStepDegrees = 6f;                // 圆形边界时弧线细分角度
    [SerializeField] private bool useCameraRadius = true;              // 是否使用相机可见范围作为扫描半径
    [SerializeField] private float scanRadius = 12f;                   // 手动指定的扫描半径（useCameraRadius = false 时生效）
    [SerializeField] private float screenMargin = 1f;                  // 相机边界额外扩展距离，防止边缘闪烁

    [SerializeField] private bool useCircleBoundary = false;           // 是否使用正圆形边界（true＝圆形视野，false＝射线延伸）
    [SerializeField] private bool useFacingCull = true;                // 是否只处理面向玩家的墙体边缘（背对玩家的墙不生成遮罩）
    [SerializeField] private string groundTilemapName = "Ground";      // 地面 Tilemap GameObject 名字
    //[SerializeField] private bool groundUseFacingCull = true;          // 地面延伸是否也进行面向剔除
    //[SerializeField] private int groundSortingOrderOffset = -1;        // 地面延伸 Mesh 的 sortingOrder 相对偏移（通常放地面下面）

    // ==================== 【运行时引用】 ====================

    private CompositeCollider2D compositeCollider;       // 墙体复合碰撞体（用于取边缘路径）
    private Tilemap wallTilemap;
    private TilemapRenderer wallTileRenderer;

    private CompositeCollider2D groundCompositeCollider; // 地面复合碰撞体
    private Tilemap groundTilemap;
    private TilemapRenderer groundTileRenderer;

    private MeshFilter meshFilter;                       // 墙体遮罩 Mesh
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    //private MeshFilter groundMeshFilter;                 // 主地面延伸 Mesh（当前未使用，已废弃）
    private MeshRenderer groundMeshRenderer;
    private Mesh groundMesh;

    // 每条面向玩家的地面边缘单独生成一个 Mesh，方便按距离排序显示（近的覆盖远的）
    private List<MeshFilter> groundEdgeFilters = new List<MeshFilter>();
    private List<MeshRenderer> groundEdgeRenderers = new List<MeshRenderer>();
    private List<Mesh> groundEdgeMeshes = new List<Mesh>();

    //private CircleCollider2D playerCollider;             // 玩家的圆形碰撞体（目前仅用于 Awake 中获取）

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

        // 自动查找地面 Tilemap
        var groundObj = GameObject.Find(groundTilemapName);
        if (groundObj)
        {
            groundCompositeCollider = groundObj.GetComponent<CompositeCollider2D>();
            groundTilemap = groundObj.GetComponent<Tilemap>();
            groundTileRenderer = groundObj.GetComponent<TilemapRenderer>();
        }

        var shader = Shader.Find("Sprites/Default");
        var sr = GetComponentInChildren<SpriteRenderer>();

        // ------------------- 创建墙体遮罩物体 -------------------
        var wallGo = new GameObject("VisibilityMesh");
        wallGo.transform.SetParent(null);
        meshFilter = wallGo.AddComponent<MeshFilter>();
        meshRenderer = wallGo.AddComponent<MeshRenderer>();

        // 颜色同步：尽量取墙体 TilemapRenderer 的材质颜色
        var wallMat = new Material(shader);
        // Color wallCol = wallTileRenderer != null && wallTileRenderer.material != null
        //     ? wallTileRenderer.material.color
        //     : (wallTilemap ? wallTilemap.color : Color.gray);
        Color wallCol = wallTilemap ? wallTilemap.color : Color.gray;
        fillColor = new Color(wallCol.r, wallCol.g, wallCol.b, 1f);
        wallMat.color = fillColor;
        meshRenderer.material = wallMat;

        if (sr) meshRenderer.sortingLayerID = sr.sortingLayerID;
        meshRenderer.sortingOrder = sortingOrder;

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        meshFilter.sharedMesh = mesh;

        
        // // ------------------- 创建地面延伸主物体（已废弃，仅保留兼容） -------------------
        // var groundGo = new GameObject("GroundExtensionMesh");
        // groundGo.transform.SetParent(null);
        // groundMeshFilter = groundGo.AddComponent<MeshFilter>();
        // groundMeshRenderer = groundGo.AddComponent<MeshRenderer>();

        // var groundMat = new Material(shader);
        // Color groundCol = groundTileRenderer != null && groundTileRenderer.material != null
        //     ? groundTileRenderer.material.color
        //     : (groundTilemap ? groundTilemap.color : Color.gray);
        // groundMat.color = new Color(groundCol.r, groundCol.g, groundCol.b, 1f);
        // groundMeshRenderer.material = groundMat;

        // if (groundTileRenderer != null)
        // {
        //     groundMeshRenderer.sortingLayerID = groundTileRenderer.sortingLayerID;
        //     groundMeshRenderer.sortingOrder = groundTileRenderer.sortingOrder + groundSortingOrderOffset;
        // }
        // else if (sr)
        // {
        //     groundMeshRenderer.sortingLayerID = sr.sortingLayerID;
        //     groundMeshRenderer.sortingOrder = sortingOrder + groundSortingOrderOffset;
        // }

        // groundMesh = new Mesh();
        // groundMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        // groundMesh.MarkDynamic();
        // groundMeshFilter.sharedMesh = groundMesh;
        

        // 默认墙体层掩码
        //if (wallLayerMask == -1) wallLayerMask = LayerMask.GetMask("Wall");
    }

    void OnDestroy()
    {
        // 彻底销毁运行时创建的所有资源，防止泄露
        if (mesh != null) Destroy(mesh);
        if (meshRenderer != null && meshRenderer.material != null) Destroy(meshRenderer.material);
        if (meshFilter != null && meshFilter.gameObject != null) Destroy(meshFilter.gameObject);

        if (groundMesh != null) Destroy(groundMesh);
        if (groundMeshRenderer != null && groundMeshRenderer.material != null) Destroy(groundMeshRenderer.material);
        // if (groundMeshFilter != null && groundMeshFilter.gameObject != null) Destroy(groundMeshFilter.gameObject);

        // 销毁所有单独的地面边缘 Mesh
        for (int i = 0; i < groundEdgeFilters.Count; i++)
        {
            if (groundEdgeMeshes[i] != null) Destroy(groundEdgeMeshes[i]);
            if (groundEdgeRenderers[i] != null && groundEdgeRenderers[i].material != null) 
                Destroy(groundEdgeRenderers[i].material);
            if (groundEdgeFilters[i] != null && groundEdgeFilters[i].gameObject != null) 
                Destroy(groundEdgeFilters[i].gameObject);
        }
        groundEdgeFilters.Clear();
        groundEdgeRenderers.Clear();
        groundEdgeMeshes.Clear();
    }

    void LateUpdate()
    {
        if (compositeCollider == null && groundCompositeCollider == null) return;

        var center = (Vector2)transform.position;

        // 动态同步墙体/地面颜色变化（如白天黑夜切换）
        UpdateFillColorFromWall();
        //UpdateFillColorFromGround();

        // 计算本次需要扫描的半径（相机自适应 or 手动）
        float r;
        if (useCameraRadius && Camera.main != null)
        {
            var halfH = Camera.main.orthographicSize;
            var halfW = halfH * Camera.main.aspect;
            r = Mathf.Sqrt(halfW * halfW + halfH * halfH) + screenMargin;
        }
        else
        {
            r = scanRadius;
        }

        BuildPerEdgeMesh(center, r);
    }

    // ==================== 【颜色同步】 ====================

    void UpdateFillColorFromWall()
    {
        // Color wallCol = Color.gray;
        // if (wallTileRenderer != null && wallTileRenderer.material != null)
        //     wallCol = wallTileRenderer.material.color;
        // else if (wallTilemap != null)
        //     wallCol = wallTilemap.color;
        Color wallCol = wallTilemap ? wallTilemap.color : Color.gray;

        fillColor = new Color(wallCol.r, wallCol.g, wallCol.b, 1f);
        if (meshRenderer != null && meshRenderer.material != null)
            meshRenderer.material.color = fillColor;
    }

    void UpdateFillColorFromGround()
    {
        Color gc = Color.gray;
        if (groundTileRenderer != null && groundTileRenderer.material != null)
            gc = groundTileRenderer.material.color;
        else if (groundTilemap != null)
            gc = groundTilemap.color;

        var col = new Color(gc.r, gc.g, gc.b, 1f);
        if (groundMeshRenderer != null && groundMeshRenderer.material != null)
            groundMeshRenderer.material.color = col;

        // 同步所有单独的地面边缘材质
        for (int i = 0; i < groundEdgeRenderers.Count; i++)
        {
            var r = groundEdgeRenderers[i];
            if (r != null && r.material != null) r.material.color = col;
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

                // 过远边缘直接跳过，优化性能
                if ((v1 - center).sqrMagnitude > rangeRadius * rangeRadius * 4f && 
                    (v2 - center).sqrMagnitude > rangeRadius * rangeRadius * 4f) continue;

                var a1 = Mathf.Atan2(v1.y - center.y, v1.x - center.x);
                var a2 = Mathf.Atan2(v2.y - center.y, v2.x - center.x);

                // 【面向剔除】只处理面向玩家的墙面（背对的不生成遮罩）
                if (useFacingCull)
                {
                    var mid = (v1 + v2) * 0.5f;
                    var toP = center - mid;
                    var dir = (v2 - v1).normalized;
                    var nrm = new Vector2(-dir.y, dir.x); // 边缘法线（向左）
                    if (Vector2.Dot(nrm, toP) <= 0f) continue;
                }

                //var far = GetFarLength();
                var far = rayLength;

                // 边界点：圆形边界 or 射线延伸
                var e1 = useCircleBoundary
                    ? center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius
                    : center + (v1 - center).normalized * far;

                var e2 = useCircleBoundary
                    ? center + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * radius
                    : center + (v2 - center).normalized * far;

                // 如果使用圆形边界，需要在两点之间插入弧线顶点
                var arc = new List<Vector3>();
                if (useCircleBoundary)
                {
                    var deltaDeg = Mathf.DeltaAngle(a1 * Mathf.Rad2Deg, a2 * Mathf.Rad2Deg);
                    var step = Mathf.Sign(deltaDeg) * arcStepDegrees;
                    arc.Add(e2);
                    var deg2 = a2 * Mathf.Rad2Deg;
                    for (float d = 0f; Mathf.Abs(d) < Mathf.Abs(deltaDeg) - 0.001f; d += step)
                    {
                        var ang = (deg2 + d) * Mathf.Deg2Rad;
                        var cp = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
                        arc.Add(cp);
                    }
                    arc.Add(e1);
                }

                // 构造本条边缘的多边形：v1 → v2 → (arc or e2→e1) → 返回
                var poly = new List<Vector3> { v1, v2 };
                if (useCircleBoundary)
                    poly.AddRange(arc);
                else
                {
                    poly.Add(e2);
                    poly.Add(e1);
                }

                var baseIndex = wallVerts.Count;
                for (int t = 0; t < poly.Count; t++)
                {
                    wallVerts.Add(poly[t]);
                    wallColors.Add(fillColor);
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
        /*
        // ------------------- 处理地面延伸（可见区域外仍显示地面） -------------------
        if (groundCompositeCollider != null)
        {
            var edges = new List<(Vector3 v1, Vector3 v2, Vector3 e2, Vector3 e1, float dist)>();

            var countG = groundCompositeCollider.pathCount;
            for (int i = 0; i < countG; i++)
            {
                var n = groundCompositeCollider.GetPathPointCount(i);
                if (n < 2) continue;

                var arr = new Vector2[n];
                groundCompositeCollider.GetPath(i, arr);
                var worldPoly = new List<Vector2>(n);
                for (int k = 0; k < n; k++)
                    worldPoly.Add((Vector2)groundCompositeCollider.transform.TransformPoint(arr[k]));

                // 如果玩家站在这个地面多边形内部，则不向外延伸（防止地面覆盖玩家）
                if (PointInPolygon(center, worldPoly)) continue;

                for (int k = 0; k < n; k++)
                {
                    var gv1 = worldPoly[k];
                    var gv2 = worldPoly[(k + 1) % n];

                    // 地面面向剔除
                    if (groundUseFacingCull)
                    {
                        var mid = (gv1 + gv2) * 0.5f;
                        var toP = center - mid;
                        var dir = (gv2 - gv1).normalized;
                        var nrm = new Vector2(-dir.y, dir.x);
                        if (Vector2.Dot(nrm, toP) <= 0f) continue;
                    }

                    var far = GetFarLength();

                    var ge1 = useCircleBoundary
                        ? center + (center - gv1).normalized * radius
                        : gv1 + (center - gv1).normalized * far;

                    var ge2 = useCircleBoundary
                        ? center + (center - gv2).normalized * radius
                        : gv2 + (center - gv2).normalized * far;

                    float d = DistanceToSegment(center, gv1, gv2);
                    edges.Add((gv1, gv2, ge2, ge1, d));
                }
            }

            // 按距离玩家由近到远排序 → 近的在上面渲染（sortingOrder 越大越上层）
            edges.Sort((a, b) => a.dist.CompareTo(b.dist));
            int needed = edges.Count;

            int layerId = groundTileRenderer != null 
                ? groundTileRenderer.sortingLayerID 
                : (GetComponentInChildren<SpriteRenderer>() ? GetComponentInChildren<SpriteRenderer>().sortingLayerID : 0);

            int baseOrder = groundTileRenderer != null 
                ? groundTileRenderer.sortingOrder + groundSortingOrderOffset 
                : sortingOrder + groundSortingOrderOffset;

            // 动态创建/复用足够的 Mesh 对象
            for (int i = groundEdgeFilters.Count; i < needed; i++)
            {
                var go = new GameObject("GroundExt_" + i);
                go.transform.SetParent(null);
                var gf = go.AddComponent<MeshFilter>();
                var gr = go.AddComponent<MeshRenderer>();
                var shader = Shader.Find("Sprites/Default");
                var mat = new Material(shader);
                gr.material = mat;
                gr.sortingLayerID = layerId;

                groundEdgeFilters.Add(gf);
                groundEdgeRenderers.Add(gr);

                var gm = new Mesh();
                gm.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                gm.MarkDynamic();
                gf.sharedMesh = gm;
                groundEdgeMeshes.Add(gm);
            }

            // 隐藏多余的 Mesh
            for (int i = 0; i < groundEdgeFilters.Count; i++)
            {
                bool active = i < needed;
                if (groundEdgeFilters[i] != null && groundEdgeFilters[i].gameObject != null)
                    groundEdgeFilters[i].gameObject.SetActive(active);
            }

            // 为每条边缘生成一个四边形 Mesh
            for (int i = 0; i < needed; i++)
            {
                var e = edges[i];

                var verts = new Vector3[4] { e.v1, e.v2, e.e2, e.e1 };
                var tris = new int[6] { 0, 1, 2, 0, 2, 3 };

                Color gc = groundTileRenderer != null && groundTileRenderer.material != null 
                    ? groundTileRenderer.material.color 
                    : (groundTilemap ? groundTilemap.color : Color.white);
                var col = new Color(gc.r, gc.g, gc.b, 1f);
                var cols = new Color[4] { col, col, col, col };

                var gm = groundEdgeMeshes[i];
                gm.Clear();
                gm.vertices = verts;
                gm.colors = cols;
                gm.triangles = tris;
                gm.RecalculateBounds();

                var gr = groundEdgeRenderers[i];
                gr.sortingLayerID = layerId;
                // 距离越近，sortingOrder 越大 → 越在上层显示
                int order = baseOrder - (needed - i);
                gr.sortingOrder = order;

                groundEdgeFilters[i].transform.position = Vector3.zero;
            }
        }
        */

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

        
        // // 主地面延伸 Mesh 已废弃，保持清空
        // if (groundMesh != null)
        // {
        //     groundMesh.Clear();
        //     groundMesh.vertices = System.Array.Empty<Vector3>();
        //     groundMesh.colors = System.Array.Empty<Color>();
        //     groundMesh.triangles = System.Array.Empty<int>();
        //     groundMesh.RecalculateBounds();
        //     groundMeshFilter.transform.position = Vector3.zero;
        // }
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