using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMove : MonoBehaviour
{
    public CaculateNodes wayNodes;
    int leftNode = -1;
    int rightNode = -1;

    public List<Transform> wayNodeList = new List<Transform>();
    public List<Transform> rotateNodeList = new List<Transform>();

    private void Start()
    {
        //wayNodeList = wayNodes.wayNodeList;
       // rotateNodeList = wayNodes.rotateNodeList;

        if (wayNodeList.Count >= 2)
        {
            var tf0 = wayNodeList[0];
            var tf1 = wayNodeList[1];

            transform.position = tf0.position;
            transform.right = (tf1.position - tf0.position).normalized;
            
            leftNode = 0;
            rightNode = 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.A) == true)
            MoveOnWall("A");

        if (Input.GetKey(KeyCode.D) == true)
            MoveOnWall("D");
    }

    void MoveOnWall(string key)
    {
        if (key != "A" && key != "D")
            return;

        var moveDistance = Time.deltaTime * 0.5f;

        var direction = (key == "A" ? -1 : 1);
        var targetPos = (direction == -1 ? wayNodeList[leftNode].position : wayNodeList[rightNode].position);
        var targetDistance = Vector3.Distance(targetPos, transform.position);

        transform.position += direction * transform.right * moveDistance;
        
        if (moveDistance >= targetDistance)
        {
            transform.position = targetPos;

            //BUG wrong
            if(leftNode != 0 && rightNode != wayNodeList.Count - 1)
            {
                leftNode += direction;
                rightNode += direction;
            }

            transform.right = (wayNodeList[rightNode].position - wayNodeList[leftNode].position).normalized;
        }
    }
}
