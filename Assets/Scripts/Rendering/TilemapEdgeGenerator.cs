using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TilemapEdgeGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public GameObject wallPrefab; // 3D墙体的预制体
    public float wallHeight = 20f; // 墙的高度
    public int heightNum = 1; // 墙的高度数量
    
    // 新增：用于跟踪已生成的墙体，防止内存泄漏
    private List<GameObject> generatedWalls = new List<GameObject>();
    private bool hasGenerated = false; // 新增：标记是否已经生成过墙体
    
    private void Start()
    {
        // 在生成墙体前调整大小
        // wallPrefab.transform.localScale = new Vector3(1, 1, wallHeight); // 这里的1是厚度，可以根据需要调整
        
        // 修改：添加检查，确保只生成一次
        Debug.Log("Start!!!!!!!!!!!!!!!!");
        ClearGeneratedWalls();
        if (!hasGenerated)
        {
            Generate3DWalls();
            hasGenerated = true; // 标记为已生成
        }
    }

    // private void FixedUpdate()
    // {
    //     ClearGeneratedWalls();
    // }

    // 新增：清理方法，防止内存泄漏
    private void OnDestroy()
    {
        ClearGeneratedWalls();
    }
    
    // 新增：手动清理生成墙体的方法
    [ContextMenu("Clear Generated Walls")]
    public void ClearGeneratedWalls()
    {
        foreach (GameObject wall in generatedWalls)
        {
            if (wall != null)
            {
                // 根据运行模式选择销毁方式
                if (Application.isPlaying)
                    Destroy(wall);
                else
                    DestroyImmediate(wall);
            }
        }
        generatedWalls.Clear();
        hasGenerated = false; // 重置生成标记
        Debug.Log("已清理所有生成的墙体");
    }
    
    // 新增：重新生成墙体的方法
    [ContextMenu("Regenerate Walls")]
    public void RegenerateWalls()
    {
        ClearGeneratedWalls();
        Generate3DWalls();
        hasGenerated = true;
    }
    
    public void Generate3DWalls()
    {
        ClearGeneratedWalls();
        // 获取Tilemap的所有有瓦片的位置
        List<Vector3Int> allTilePositions = new List<Vector3Int>();
        
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                allTilePositions.Add(pos);
            }
        }
        
        // 检测每个瓦片的四个方向是否是边缘
        foreach (var pos in allTilePositions)
        {
            CheckAndGenerateWall(pos, Vector3Int.up);    // 上
            CheckAndGenerateWall(pos, Vector3Int.down);    // 下
            CheckAndGenerateWall(pos, Vector3Int.left);    // 左
            CheckAndGenerateWall(pos, Vector3Int.right);  // 右
        }
        
        Debug.Log($"生成了 {generatedWalls.Count} 个墙体"); // 新增：调试信息
    }
    
    private void CheckAndGenerateWall(Vector3Int position, Vector3Int direction)
    {
        // 检查相邻位置是否有瓦片
        Vector3Int adjacentPos = position + direction;
        
        if (!tilemap.HasTile(adjacentPos))
        {
            // 这是边缘，生成3D墙体
            Vector3 worldPos = tilemap.GetCellCenterWorld(position);
            Vector3 wallPos = worldPos + (Vector3)direction * 0f + Vector3.forward * wallHeight * 0.5f +Vector3.forward * 0.01f;
            GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity);
            //###############多箱子方案
            // List<GameObject> allWalls = new List<GameObject>();
            // for (int i = 0; i < heightNum; i++)
            // {
            //     GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity);
            //     // wall.transform.localScale = new Vector3(1, 1, wallHeight);
                
            //     // 修改：设置父对象便于管理
            //     wall.transform.SetParent(this.transform);
                
            //     // 修改：添加到全局列表中进行跟踪
            //     generatedWalls.Add(wall);
            //     allWalls.Add(wall);
            //     wallPos += Vector3.forward * wallHeight * 1f;
            // }

            // allWalls.Add(wall);
            
            // // 根据方向旋转墙体
            // if (direction == Vector3Int.up || direction == Vector3Int.down)
            // {
            //     wall.transform.rotation = Quaternion.Euler(0, 0, 90);
            // }
            
            // 调整墙体大小
            // wall.transform.localScale = new Vector3(1, 1, wallHeight);
        }
    }
}