using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleCtrl : MonoBehaviour
{
    Vector2 moveDir;
    float rotateFactor = 80f;
    public Rigidbody rigidBody;
    public Animation animation;
    public float cameraHeight;
    public float cameraDistance;
    public Transform pivot;
    public Projector projector;
    public GameObject effect;

    public CaculateNodes wayNodes;
    WayPointManager wayNodeManager;

    private Coroutine rotate;
    private Coroutine move;
    private Coroutine cameraFollow;
    private Coroutine tryProject = null;

    // Start is called before the first frame update
    void Start()
    {
        effect.SetActive(false);
        Camera.main.transform.forward = transform.forward;

        wayNodeManager = wayNodes.nodeManager;
        ActiveCameraFollow(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(tryProject == null)
            {
                animation.Play("Idle");
                tryProject = StartCoroutine(TryProject());
            }
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        moveDir.x = (Input.GetAxisRaw("Vertical") != 0 ? Input.GetAxisRaw("Vertical") : 0);
        moveDir.y = (Input.GetAxisRaw("Horizontal") != 0 ? Input.GetAxisRaw("Horizontal") : 0);
    }

    IEnumerator UpdateCurrentSpeed()
    {
        while (true)
        {
            float distance = Time.deltaTime * 0.5f;

            if (moveDir.x == 0)
            {
                animation.Play("Idle");
            }

            if (moveDir.x > 0)
            {
                rigidBody.MovePosition(rigidBody.position + transform.forward * distance);
                animation.Play("Run");
            }

            if (moveDir.x < 0)
            {
                rigidBody.MovePosition(rigidBody.position - transform.forward * distance);
                animation.Play("Run_Back");
            }

            yield return null;
        }
    }

    IEnumerator UpdateRotation()
    {
        while (true)
        {
            if (moveDir.y == 0)
            {
                yield return null;
                continue;
            }

            var temp = transform.eulerAngles;

            if (moveDir.x >= 0)
            {
                temp.y = temp.y + moveDir.y * rotateFactor * Time.deltaTime;
                //animation.Play("Left");
            }

            if (moveDir.x < 0)
            {
                temp.y = temp.y - moveDir.y * rotateFactor * Time.deltaTime;
                //animation.Play("Right");
            }

            transform.eulerAngles = temp;
            yield return null;
        }
    }

    IEnumerator CameraFollow()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var cameraTransform = Camera.main.transform;

        while (true)
        {
            float h = Input.GetAxis("Mouse X") * 4;
            float v = Input.GetAxis("Mouse Y") * 4;

            cameraTransform.RotateAround(transform.position, Vector3.up, h);

            var pos = transform.position - cameraTransform.forward * cameraDistance;
            pos.y = transform.position.y + cameraHeight;
            cameraTransform.position = pos;

            yield return null;
        }
    }

    void ActiveCameraFollow(bool bo)
    {
        if(bo == true)
        {
            rotate = StartCoroutine(UpdateRotation());
            move = StartCoroutine(UpdateCurrentSpeed());
            cameraFollow = StartCoroutine(CameraFollow());
        }

        if (bo == false)
        {
            StopCoroutine(rotate);
            StopCoroutine(move);
            StopCoroutine(cameraFollow);
        }
    }

    IEnumerator TryProject()
    {
        GameObject wall = null;
        float wallDistance = -1;

        var bo = GetTouchWall(ref wall, ref wallDistance);
        if (bo == false)
            yield break;
        
        ActiveCameraFollow(false);
        var mark = wall.transform.Find("Mark");

        yield return StartCoroutine(StartProject(mark.transform, wallDistance));
        yield return StartCoroutine(ProjectOnWall(wallDistance));
        yield return StartCoroutine(EndProject());

        yield return new WaitForSeconds(1f);

        ActiveCameraFollow(true);
        tryProject = null;
    }

    IEnumerator StartProject(Transform wallNormal, float wallDistance)
    {
        Debug.Log("StartProject");

        var projectCameraPos = pivot.position + wallNormal.forward * (cameraDistance * 2 - wallDistance);
        var projectCameraRotation = Quaternion.LookRotation(wallNormal.forward * -1);

        // Lerp main camera pos & rotation to projector.
        var originalCameraPos = Camera.main.transform.position;
        var originalCameraRotation = Camera.main.transform.rotation;
        var originalZ = transform.localScale.z;

        var totalTime = 1.0f;
        var deltaTime = 0.01f;
        var loopTimes = totalTime / deltaTime;
        
        var delay = new WaitForSeconds(deltaTime);
        
        for (float i = 0; i < loopTimes; i++)
        {
            var z = Mathf.Lerp(originalZ, 0, i / loopTimes);
            var localScale = transform.localScale;
            localScale.z = z;
            transform.localScale = localScale;

            Camera.main.transform.position = Vector3.Lerp(originalCameraPos, projectCameraPos, i / loopTimes);
            Camera.main.transform.rotation = Quaternion.Lerp(originalCameraRotation, projectCameraRotation, i / loopTimes);

            yield return delay;
        }

        var originalRotation = transform.rotation;

        for (float i = 0; i < loopTimes; i++)
        {
            transform.rotation = Quaternion.Lerp(originalRotation, wallNormal.rotation, i / loopTimes);
            yield return delay;
        }

        StartCoroutine(PlayEffect(pivot.position + wallNormal.forward * 0.2f));
        yield return new WaitForSeconds(0.2f);
        SetModelVisible(false);

        Debug.Log(transform.position);
        transform.position += transform.forward * wallDistance;
        Debug.Log(transform.position);
    }

    IEnumerator PlayEffect(Vector3 pos)
    {
        effect.transform.position = pivot.position;
        effect.SetActive(true);
        yield return new WaitForSeconds(0.6f);
        effect.SetActive(false);
    }

    IEnumerator ProjectOnWall(float wallDistance)
    {
        var leftIndex = -1;
        var rightIndex = -1;

        bool bo = wayNodeManager.GetBothEndIndex(transform, ref leftIndex, ref rightIndex); //todo!!!!!
        if (bo == false)
            yield break;

        Debug.Log(rightIndex);

        transform.right = wayNodeManager.GetDirection(leftIndex, rightIndex);
        projector.enabled = true;

        var deltaTime = 0.005f;
        var delay = new WaitForSeconds(deltaTime);

        while(true)
        {
            if(Input.GetKey(KeyCode.Space) == true)
                yield break;

            var targetIndex = -1;

            if (Input.GetKey(KeyCode.A) == true)
                targetIndex = leftIndex;
            
            if (Input.GetKey(KeyCode.D) == true)
                targetIndex = rightIndex;
            
            if (targetIndex == -1)
            {
                yield return delay;
                continue;
            }

            Camera.main.transform.position = pivot.position + -1 * transform.forward * (cameraDistance * 2 - wallDistance);
            
            var targetPos = wayNodeManager.GetPosByIndex(targetIndex);
            targetPos.y = transform.position.y;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, deltaTime * 0.5f);

            if (Vector3.Distance(transform.position, targetPos) < 0.0001)
            {
                if (targetIndex == leftIndex && leftIndex != 0)
                {
                    leftIndex -= 1;
                    rightIndex -= 1;
                }

                else if (targetIndex == rightIndex && rightIndex != wayNodeManager.wayPointList.Count - 1)
                {
                    leftIndex += 1;
                    rightIndex += 1;
                }

                transform.right = wayNodeManager.GetDirection(leftIndex, rightIndex);
                Camera.main.transform.rotation = Quaternion.LookRotation(transform.forward);
            }

            yield return delay;
        }
    }




    IEnumerator EndProject()
    {
        StartCoroutine(PlayEffect(pivot.position + -1 * transform.forward * 0.2f));
        yield return new WaitForSeconds(0.2f);
        
        projector.enabled = false;
        transform.rotation = Quaternion.LookRotation(-1 * transform.forward);
        SetModelVisible(true);

        var totalTime = 1.0f;
        var deltaTime = 0.01f;

        var delay = new WaitForSeconds(deltaTime);
        var loopTimes = totalTime / deltaTime;
        var originalZ = transform.localScale.z;

        for (float i = 0; i < loopTimes; i++)
        {
            var z = Mathf.Lerp(originalZ, 1, i / loopTimes);
            var localScale = transform.localScale;
            localScale.z = z;
            transform.localScale = localScale;

            yield return delay;
        }
    }

    void SetModelVisible(bool bo)
    {
        transform.Find("Belt_02").gameObject.SetActive(bo);
        transform.Find("Pelvis").gameObject.SetActive(bo);
        transform.Find("SpaceMan_Base").gameObject.SetActive(bo);

        rigidBody.isKinematic = !bo;
    }

    bool GetTouchWall(ref GameObject wall, ref float distance)
    {
        var results = Physics.OverlapSphere(transform.position, 0.25f, LayerMask.GetMask("Wall"));
        if (results.Length == 0)
        {
            Debug.Log("Not touch any wall!");
            return false;
        }

        float nearestDis = 999999;
        for (int i = 0; i < results.Length; i++)
        {
            var touchPos = results[i].ClosestPointOnBounds(transform.position);
            distance = Vector3.Distance(touchPos, transform.position);
            if (nearestDis >= distance)
            {
                nearestDis = distance;
                wall = results[i].gameObject;
            }
        }

        return true;
    }
}
