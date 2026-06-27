using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    public GameObject cloude;
    public GameObject cloude2;
    public GameObject cloude3;

    public float cloudeMoveSpeed; 
    public float returnPositionX = 1430f; 
    public float startPositionX = -1430f; 

    public void Update()
    {
        if (cloude != null || cloude2 != null || cloude3 != null)
        {
            MoveAndReturnCloud(cloude);
            MoveAndReturnCloud(cloude2);
            MoveAndReturnCloud(cloude3);
        }

    }

    private void MoveAndReturnCloud(GameObject cloudObject)
    {
        cloudObject.transform.Translate(Vector3.right * cloudeMoveSpeed * Time.deltaTime);

        if (cloudObject.transform.position.x >= returnPositionX)
        {
            cloudObject.transform.position = new Vector3(startPositionX, cloudObject.transform.position.y, cloudObject.transform.position.z);
        }
    }

}