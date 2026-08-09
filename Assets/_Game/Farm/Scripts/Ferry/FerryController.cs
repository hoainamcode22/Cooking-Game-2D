using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Thuyen du lich chay tren bien theo waypoint, dung ben don/tra khach.
/// - Waypoint dau & cuoi = 2 ben tau (DockA / DockB). O giua = duong bien.
/// - Toi ben: dung dockWaitTime giay (don/tra khach) roi chay tiep (ping-pong A<->B).
/// - Visual co hieu ung dap denh (bob) + tu flip theo huong chay.
/// - Hook su kien onArriveDock / onDepartDock de noi voi he thong khach/mission.
/// </summary>
public class FerryController : MonoBehaviour
{
    [Header("Duong di (keo tha cac diem con WP_*)")]
    public Transform[] waypoints;

    [Header("Chuyen dong")]
    public float speed = 300f;              // don vi/giay (map dung he toa do lon nhu tau lua)
    public float dockWaitTime = 5f;         // thoi gian dau ben
    public float arriveThreshold = 3f;

    [Header("Hieu ung")]
    public SpriteRenderer visual;           // sprite thuyen (child "Visual")
    public float bobAmplitude = 8f;         // bien do dap denh (don vi world)
    public float bobFrequency = 0.8f;
    public bool spriteFacesLeft = false;    // sprite goc quay mat sang trai?

    [Header("Khach du lich")]
    public int passengerCapacity = 8;
    public int passengers;

    [Header("Su kien")]
    public UnityEvent<int> onArriveDock;    // int = index waypoint ben
    public UnityEvent<int> onDepartDock;

    int index;
    int dir = 1;
    bool waiting;
    float bobT;

    void Start()
    {
        // Tu dong tim waypoint neu user chua gan
        if (waypoints == null || waypoints.Length < 2 || waypoints[0] == null || waypoints[waypoints.Length - 1] == null)
        {
            var foundWP = new System.Collections.Generic.List<Transform>();
            
            // Tim toan bo GameObject trong Scene, loc nhung ten bat dau bang WP_
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.StartsWith("WP_"))
                    foundWP.Add(obj.transform);
            }
            
            // Sap xep theo ten (WP_1, WP_2, ...)
            foundWP.Sort((a, b) => a.name.CompareTo(b.name));
            
            if (foundWP.Count >= 2)
                waypoints = foundWP.ToArray();
        }

        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
            transform.position = waypoints[0].position;
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length < 2 || waiting) { Bob(); return; }

        Transform target = waypoints[index + dir];
        if (target == null) return; // Tranh loi UnassignedReferenceException

        Vector3 to = target.position - transform.position;
        Vector3 step = to.normalized * speed * Time.deltaTime;
        
        if (step.sqrMagnitude >= to.sqrMagnitude || to.magnitude < arriveThreshold)
        {
            transform.position = target.position;
            index += dir;
            if (index == 0 || index == waypoints.Length - 1)
                StartCoroutine(DockRoutine());
        }
        else
        {
            transform.position += step;
        }
        Bob();
    }

    IEnumerator DockRoutine()
    {
        waiting = true;
        onArriveDock?.Invoke(index);
        passengers = (index == waypoints.Length - 1) ? 0 : passengerCapacity;
        yield return new WaitForSeconds(dockWaitTime);
        dir = (index == 0) ? 1 : -1;        // ping-pong
        onDepartDock?.Invoke(index);
        waiting = false;
    }

    void Bob()
    {
        if (visual == null) return;
        bobT += Time.deltaTime;
        float s = Mathf.Max(0.0001f, transform.lossyScale.y);
        var lp = visual.transform.localPosition;
        lp.y = Mathf.Sin(bobT * bobFrequency * Mathf.PI * 2f) * bobAmplitude / s;
        visual.transform.localPosition = lp;
        // Da xoa dong xoay thuyen theo yeu cau: "khong can xoay hay gi het"
    }

    // Ve duong line LUON HIEN trong Scene view (giong duong ray tau lua) de de noi diem
    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            bool isDock = (i == 0 || i == waypoints.Length - 1);
            Gizmos.color = isDock ? Color.yellow : Color.cyan;
            Gizmos.DrawSphere(waypoints[i].position, isDock ? 0.22f : 0.15f);
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}
