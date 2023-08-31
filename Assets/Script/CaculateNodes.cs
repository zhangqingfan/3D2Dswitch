using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WayPointManager
{
    public class RotateNodeInfo
    {
        public Transform transform;
        public int wayNodeIndex;
    }

    public List<Transform> wayPointList = new List<Transform>();
    public List<RotateNodeInfo> rotatePointList = new List<RotateNodeInfo>();

    bool TryStartRotate(int index, Vector3 pos, float distance, ref Vector3 centerPoint)
    {
        var dis = Vector3.Distance(pos, wayPointList[index].position);

        if (dis > distance)
            return false;
    
        for (int i = 0; i < rotatePointList.Count; i++)
        {
            if (rotatePointList[i].wayNodeIndex == index)
            {
                centerPoint = rotatePointList[i].transform.position;
                return true;
            }
        }

        return false;
    }

    public bool GetBothEndIndex(Transform targetTransform, ref int leftIndex,  ref int rightIndex)
    {
        leftIndex = -1;
        rightIndex = -1;
        var minLeftDis = 99999f;
        var minRightDis = 99999f;

        for (int i = 0; i < wayPointList.Count; i++)
        {
            var dir = (wayPointList[i].position - targetTransform.position).normalized;
            var side = Vector3.Cross(dir, targetTransform.forward);
            var dis = Vector3.Distance(targetTransform.position, wayPointList[i].position);

            if (side.y <= 0 && dis <= minLeftDis) //left
            {
                minLeftDis = dis;
                leftIndex = i;
            }
            else if (side.y > 0 && dis <= minRightDis) //right
            {
                minRightDis = dis;
                rightIndex = i;
            }
        }

        if (leftIndex == rightIndex)
            return false;

        return true;
    }

    public Vector3 GetDirection(int fromIndex, int toIndex)
    {
        return (wayPointList[toIndex].position - wayPointList[fromIndex].position).normalized;
    }

    public Vector3 GetPosByIndex(int index)
    {
        return wayPointList[index].position;
    }

    public RotateNodeInfo GetRotateNodeByWayNodeIndex(int index)
    {
        for(int i = 0; i < rotatePointList.Count; i++)
        {
            if (rotatePointList[i].wayNodeIndex == index)
                return rotatePointList[i]; ;
        }
        return null;
    }
}

public class CaculateNodes : MonoBehaviour
{
    public WayPointManager nodeManager = new WayPointManager();

    public List<Transform> markList = new List<Transform>();
    public float distance = 0.25f;
    public float distanceRotate = 0.25f;
    
    public Transform testObj;

    private void Awake()
    {
        TrimMarksHeight();
        DrawWayNodes();
    }
    void Start()
    {
        //StartCoroutine(TestMovingTrace());
    }

    void DrawWayNodes()
    {
        if (markList.Count == 0)
            return;

        var wayNode = markList[0].position + markList[0].forward * distance;
        CreateWayNodeSphere(wayNode);

        for (int i = 0; i < markList.Count - 1; i++)
        {
            var dir0 = markList[i].forward.normalized;
            var dir1 = markList[i + 1].forward.normalized;

            CreateRotateCenterPoint(markList[i], markList[i + 1], i + 1);

            var dir = (dir0 + dir1).normalized;
            var dot = Vector3.Dot(dir1, dir);
            var dis = distance / dot;
            CreateWayNodeSphere(markList[i + 1].position + dis * dir);
        }

        var lastTranform = markList[markList.Count - 1];
        var lastPos = lastTranform.right * -1 * lastTranform.localScale.x * 2 + nodeManager.wayPointList[nodeManager.wayPointList.Count - 1].position;
        CreateWayNodeSphere(lastPos);
    }

    void CreateRotateCenterPoint(Transform t0, Transform t1, int index)
    {
        var dir0 = t0.forward.normalized;
        var dir1 = t1.forward.normalized;

        var y = Vector3.Cross(dir1, dir0).y;
        if (y > 0)
        {
            dir0 *= -1;
            dir1 *= -1;
        }

        var dir = (dir0 + dir1).normalized;
        var sin = Mathf.Sin(Vector3.Angle(dir0, dir) * Mathf.Deg2Rad);
        var dis = distanceRotate / sin;

        CreateRotateCenterPointSphere(t1.position + dis * dir, dir, index);
    }

    void CreateWayNodeSphere(Vector3 pos)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale *= 0.05f;
        sphere.transform.position = pos;
        sphere.GetComponent<MeshRenderer>().material.color = Color.red;
        //sphere.SetActive(false);

        nodeManager.wayPointList.Add(sphere.transform);
    }

    void CreateRotateCenterPointSphere(Vector3 pos, Vector3 dir, int wayNodeIndex)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale *= 0.05f;
        sphere.transform.position = pos;
        sphere.transform.forward = dir;
        sphere.GetComponent<MeshRenderer>().material.color = Color.green;

        var rotateNodeInfo = new WayPointManager.RotateNodeInfo();
        rotateNodeInfo.transform = sphere.transform;
        rotateNodeInfo.wayNodeIndex = wayNodeIndex;

        nodeManager.rotatePointList.Add(rotateNodeInfo);
    }


    void FixedUpdate()
    {
        for(int i = 0; i < markList.Count; i++)
        {
            var tf = markList[i];
            Debug.DrawRay(tf.position, tf.forward, Color.red);
        }

        TestMovingTrace();

        return;
        
        if(markList.Count != 0)
        {
            var lastTf = markList[markList.Count - 1];
            Debug.DrawRay(lastTf.position, -lastTf.right, Color.red);
        }
    }

    void TrimMarksHeight()
    {
        for (int i = 0; i < markList.Count; i++)
        {
            var height = markList[0].position.y;
            markList[i].position = new Vector3(markList[i].position.x, height, markList[i].position.z);
        }
    }

    public IEnumerator TestMovingTrace()
    {
        yield break;
        /*
        if (wayNodeList.Count <= 1)
            yield break;

        testObj.position = wayNodeList[0].position;

        Debug.Log(wayNodeList[wayNodeList.Count - 1].position);

        for (int i = 0; i < wayNodeList.Count - 1; i++)
        {
            if(testObj.position != wayNodeList[i + 1].position)
            {
                testObj.position = Vector3.Lerp(wayNodeList[i].position, wayNodeList[i + 1].position, Time.deltaTime * 0.1f);
                yield return new WaitForSeconds(0.1f);
            }
        }
        */
    }
}