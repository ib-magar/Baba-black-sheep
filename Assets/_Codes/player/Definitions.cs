using UnityEngine;

[System.Serializable]
public struct InteractionData
{
    public Vector3 direction;
    public Vector3 playerCurrentPosition;
    public Vector3 playerTargetPosition;
    public GameObject playerObject;

    public InteractionData(Vector3 dir, Vector3 currentPos, Vector3 targetPos, GameObject playerObj)
    {
        direction = dir;
        playerCurrentPosition = currentPos;
        playerTargetPosition = targetPos;
        playerObject = playerObj;
    }
}

