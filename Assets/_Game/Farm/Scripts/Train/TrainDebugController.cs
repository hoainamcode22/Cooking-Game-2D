using UnityEngine;

public class TrainDebugController : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[Train Debug] Space pressed → DepartToProcess");
            TrainManager.Instance?.GetComponent<TrainPathFollower>()?.DepartToProcess();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[Train Debug] R pressed → ResetMove");
            TrainManager.Instance?.GetComponent<TrainPathFollower>()?.ResetMove();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[Train Debug] T pressed → ReturnToWait");
            TrainManager.Instance?.GetComponent<TrainPathFollower>()?.ReturnToWait();
        }
    }
}
