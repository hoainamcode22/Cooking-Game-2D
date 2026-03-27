using UnityEngine;
using UnityEngine.InputSystem;

public class SickleController : MonoBehaviour
{
    private Camera mainCam;
    private Rigidbody2D rb;

    private bool isDragging = false;

    private void Awake()
    {
        mainCam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        isDragging = true; // bật lên là kéo luôn
        Debug.Log("[Sickle] OnEnable -> auto drag ON");
    }

    public void BeginHarvestMode(Vector3 startWorldPos)
    {
        if (mainCam == null)
            mainCam = Camera.main;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        Vector3 pos = new Vector3(startWorldPos.x, startWorldPos.y, transform.position.z);

        if (rb != null)
            rb.position = pos;
        else
            transform.position = pos;

        isDragging = true; // click ô lúa xong là kéo luôn
        gameObject.SetActive(true);

        Debug.Log("[Sickle] BeginHarvestMode -> auto drag");
    }

    public void EndHarvestMode()
    {
        isDragging = false;
        Debug.Log("[Sickle] EndHarvestMode");
    }

    private void Update()
    {
        if (!isDragging)
            return;

        if (mainCam == null || Pointer.current == null)
            return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        bool isPressed = Pointer.current.press.isPressed;
        bool releasedThisFrame = Pointer.current.press.wasReleasedThisFrame;

        float camDistance = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
        Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camDistance));
        worldPos.z = transform.position.z;

        if (isPressed)
        {
            if (rb != null)
                rb.MovePosition(worldPos);
            else
                transform.position = worldPos;
        }

        if (releasedThisFrame)
        {
            isDragging = false;
            Debug.Log("[Sickle] Release -> Hide");
            FarmUIManager.Instance?.HideSickleTool();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDragging)
            return;

        PlotController plot = collision.GetComponent<PlotController>();
        if (plot == null)
            plot = collision.GetComponentInParent<PlotController>();

        if (plot == null)
            return;

        if (!plot.IsReadyToHarvest())
            return;

        string cropName = plot.CurrentCrop != null ? plot.CurrentCrop.displayName : "Nông sản";
        Debug.Log("[Sickle] Harvest -> " + cropName);

        if (plot.Harvest())
        {
            FarmManager.Instance?.OnPlotHarvested(plot, cropName);
        }
    }
}