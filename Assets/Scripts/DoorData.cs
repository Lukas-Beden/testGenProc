using UnityEngine;

public class DoorData : MonoBehaviour
{
    [SerializeField] DoorDirection _direction;
    //[SerializeField] Vector2 _offsetFromCenter;

    public DoorDirection GetDirection()
    {
        return _direction;
    }

    //public Vector2 GetOffset()
    //{
    //    return _offsetFromCenter;
    //}
}