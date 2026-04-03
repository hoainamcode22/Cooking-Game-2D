using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// TilemapExpander: Tự động mở rộng tilemap isometric khi camera tiếp cận gần mép.
/// Kiểm tra mỗi 0.5 giây, fill chunk 10x10 theo coroutine để tránh lag.
/// </summary>
public class TilemapExpander : MonoBehaviour
{
    [Header("Cấu hình Tilemap")]
    public Tilemap targetTilemap;   // Tilemap cần mở rộng (grown)
    public TileBase grassTile;      // Tile cỏ dùng để fill (tile_grass)

    [Header("Cấu hình mở rộng")]
    public int bufferTiles = 5;     // Camera cách mép < bufferTiles ô thì fill thêm
    public int chunkSize = 10;      // Mỗi lần fill đúng chunkSize x chunkSize tiles

    // Camera chính trong scene
    private Camera mainCamera;

    // Tập hợp các ô đã được fill — tránh fill trùng
    private HashSet<Vector3Int> filledCells = new HashSet<Vector3Int>();

    // Tập hợp các chunk origin đã enqueue — tránh enqueue trùng
    private HashSet<Vector3Int> enqueuedChunks = new HashSet<Vector3Int>();

    // Hàng đợi các chunk cần fill
    private Queue<Vector3Int> fillQueue = new Queue<Vector3Int>();

    // Cache bounds của vùng đã fill — cập nhật khi fill từng ô
    private int mapMinX, mapMaxX, mapMinY, mapMaxY;
    private bool boundsInitialized = false;

    // ──────────────────────────────────────────────────────────────

    void Start()
    {
        mainCamera = Camera.main;

        // Fill chunk đầu tiên tại tâm tọa độ grid
        EnqueueChunk(Vector3Int.zero);

        // Coroutine xử lý queue fill tuần tự
        StartCoroutine(ProcessFillQueue());

        // Coroutine kiểm tra định kỳ mỗi 0.5 giây
        StartCoroutine(CheckLoop());
    }

    // ──────────────────────────────────────────────────────────────
    // KIỂM TRA ĐỊNH KỲ
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Vòng lặp kiểm tra vị trí camera mỗi 0.5 giây.
    /// Không dùng Update() để giảm tải CPU.
    /// </summary>
    IEnumerator CheckLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            // Chỉ kiểm tra khi bounds đã khởi tạo (chunk đầu đã fill xong ít nhất 1 ô)
            if (boundsInitialized)
                CheckAndExpand();
        }
    }

    /// <summary>
    /// Tính vùng camera nhìn thấy → convert sang cell coordinate →
    /// so sánh với bounds tilemap → enqueue chunk mới nếu quá gần mép.
    /// </summary>
    void CheckAndExpand()
    {
        if (mainCamera == null || targetTilemap == null) return;

        // Lấy vùng nhìn thấy của camera trong world space
        Bounds camBounds = GetCameraBounds();

        // Convert 2 góc camera sang tọa độ ô của tilemap
        // (Unity xử lý scale của Grid tự động trong WorldToCell)
        Vector3Int cellMin = targetTilemap.WorldToCell(camBounds.min);
        Vector3Int cellMax = targetTilemap.WorldToCell(camBounds.max);

        // Đảm bảo min < max (isometric grid có thể đảo trục Y)
        int camMinX = Mathf.Min(cellMin.x, cellMax.x);
        int camMaxX = Mathf.Max(cellMin.x, cellMax.x);
        int camMinY = Mathf.Min(cellMin.y, cellMax.y);
        int camMaxY = Mathf.Max(cellMin.y, cellMax.y);

        // Tâm ô camera theo X và Y (dùng để căn chunk mới)
        int camCenterX = (camMinX + camMaxX) / 2;
        int camCenterY = (camMinY + camMaxY) / 2;

        // ── Hướng TRÁI: camera gần mép trái của map ──
        if (camMinX - mapMinX < bufferTiles)
        {
            // Chunk mới nằm bên trái map, căn giữa theo Y của camera
            Vector3Int origin = SnapToChunkGrid(mapMinX - chunkSize, camCenterY - chunkSize / 2);
            EnqueueChunk(origin);
        }

        // ── Hướng PHẢI: camera gần mép phải của map ──
        if (mapMaxX - camMaxX < bufferTiles)
        {
            Vector3Int origin = SnapToChunkGrid(mapMaxX, camCenterY - chunkSize / 2);
            EnqueueChunk(origin);
        }

        // ── Hướng DƯỚI: camera gần mép dưới của map ──
        if (camMinY - mapMinY < bufferTiles)
        {
            Vector3Int origin = SnapToChunkGrid(camCenterX - chunkSize / 2, mapMinY - chunkSize);
            EnqueueChunk(origin);
        }

        // ── Hướng TRÊN: camera gần mép trên của map ──
        if (mapMaxY - camMaxY < bufferTiles)
        {
            Vector3Int origin = SnapToChunkGrid(camCenterX - chunkSize / 2, mapMaxY);
            EnqueueChunk(origin);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // QUẢN LÝ CHUNK QUEUE
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Thêm chunk vào queue nếu chưa từng được enqueue.
    /// Snap origin về lưới chunk để tránh các chunk chồng lên nhau theo hướng lẻ.
    /// </summary>
    void EnqueueChunk(Vector3Int origin)
    {
        if (enqueuedChunks.Contains(origin)) return;
        enqueuedChunks.Add(origin);
        fillQueue.Enqueue(origin);
    }

    /// <summary>
    /// Snap tọa độ về bội số của chunkSize để chunk tiling đều nhau.
    /// </summary>
    Vector3Int SnapToChunkGrid(int x, int y)
    {
        int snappedX = Mathf.FloorToInt((float)x / chunkSize) * chunkSize;
        int snappedY = Mathf.FloorToInt((float)y / chunkSize) * chunkSize;
        return new Vector3Int(snappedX, snappedY, 0);
    }

    /// <summary>
    /// Xử lý queue tuần tự: lấy từng chunk ra và fill.
    /// Chờ idle nhỏ khi queue rỗng thay vì chạy liên tục.
    /// </summary>
    IEnumerator ProcessFillQueue()
    {
        while (true)
        {
            if (fillQueue.Count > 0)
            {
                Vector3Int origin = fillQueue.Dequeue();
                yield return StartCoroutine(FillChunk(origin));
            }
            else
            {
                // Queue rỗng, chờ 0.1 giây trước khi kiểm tra lại
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // FILL CHUNK
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fill đúng 1 chunk chunkSize × chunkSize tile bắt đầu từ origin.
    /// yield return null sau mỗi tile để không block main thread.
    /// </summary>
    IEnumerator FillChunk(Vector3Int origin)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector3Int cell = new Vector3Int(origin.x + x, origin.y + y, 0);

                // Chỉ fill nếu ô này chưa tồn tại trong HashSet
                if (!filledCells.Contains(cell))
                {
                    targetTilemap.SetTile(cell, grassTile);
                    filledCells.Add(cell);

                    // Cập nhật bounds cache để CheckAndExpand dùng được ngay
                    UpdateBoundsCache(cell);

                    // Nhường frame — tránh lag khi fill nhiều tile liên tiếp
                    yield return null;
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // HELPER METHODS
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Cập nhật min/max bounds theo ô vừa fill.
    /// O(1) thay vì iterate toàn bộ HashSet.
    /// </summary>
    void UpdateBoundsCache(Vector3Int cell)
    {
        if (!boundsInitialized)
        {
            mapMinX = mapMaxX = cell.x;
            mapMinY = mapMaxY = cell.y;
            boundsInitialized = true;
        }
        else
        {
            if (cell.x < mapMinX) mapMinX = cell.x;
            if (cell.x > mapMaxX) mapMaxX = cell.x;
            if (cell.y < mapMinY) mapMinY = cell.y;
            if (cell.y > mapMaxY) mapMaxY = cell.y;
        }
    }

    /// <summary>
    /// Tính vùng Bounds của camera trong world space (orthographic).
    /// </summary>
    Bounds GetCameraBounds()
    {
        float height = mainCamera.orthographicSize * 2f;
        float width  = height * mainCamera.aspect;
        return new Bounds(mainCamera.transform.position, new Vector3(width, height, 0f));
    }

    // ──────────────────────────────────────────────────────────────
    // DEBUG: Vẽ bounds map trên Scene view khi chọn object
    // ──────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (!boundsInitialized || targetTilemap == null) return;

        // Vẽ khung bounds tilemap đã fill (màu xanh lá)
        Gizmos.color = Color.green;
        Vector3 minWorld = targetTilemap.CellToWorld(new Vector3Int(mapMinX, mapMinY, 0));
        Vector3 maxWorld = targetTilemap.CellToWorld(new Vector3Int(mapMaxX, mapMaxY, 0));
        Vector3 center   = (minWorld + maxWorld) * 0.5f;
        Vector3 size     = maxWorld - minWorld;
        Gizmos.DrawWireCube(center, size);
    }
}
