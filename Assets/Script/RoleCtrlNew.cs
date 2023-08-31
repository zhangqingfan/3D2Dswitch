using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoleCtrlNew : MonoBehaviour
{
    Vector2 moveDir;
    float rotateFactor = 80f;
    public Rigidbody rigidBody;
    public Animation animation;
    public Animation projectorAnimation;
    public float cameraHeight;
    public float cameraDistance;
    public Transform pivot;
    public Projector projector;
    public GameObject effect;

    private Coroutine rotate;
    private Coroutine move;
    private Coroutine cameraFollow;
    private Coroutine tryProject = null;

    // Start is called before the first frame update
    void Start()
    {
        effect.SetActive(false);
        Camera.main.transform.forward = transform.forward;
        ActiveCameraFollow(true);

        animation.Play("Idle");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (tryProject == null)
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
        if (bo == true)
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
        Vector3 characterTurnDir = Vector3.zero;
        var wall = GetTouchWall(ref characterTurnDir);
        if (wall == null)
            yield break;

        ActiveCameraFollow(false);

        yield return StartCoroutine(StartProject(characterTurnDir));
        yield return StartCoroutine(ProjectOnWall());
        yield return StartCoroutine(EndProject());

        yield return new WaitForSeconds(1f);

        ActiveCameraFollow(true);
        tryProject = null;
    }

    IEnumerator StartProject(Vector3 characterTurnDir)
    {
        Debug.Log("StartProject");

        var projectCameraPos = pivot.position + characterTurnDir * (cameraDistance * 2);
        var projectCameraRotation = Quaternion.LookRotation(characterTurnDir * -1);

        // Lerp main camera pos & rotation to projector.
        var originalCameraPos = Camera.main.transform.position;
        var originalCameraRotation = Camera.main.transform.rotation;
        
        var totalTime = 1.0f;
        var deltaTime = 0.01f;
        var loopTimes = totalTime / deltaTime;
        var delay = new WaitForSeconds(deltaTime);
        
        for (int i = 0; i < loopTimes; i++)
        {
            SetModelLocalZ(Mathf.Lerp(1, 0, i / loopTimes));

            Camera.main.transform.position = Vector3.Lerp(originalCameraPos, projectCameraPos, i / loopTimes);
            Camera.main.transform.rotation = Quaternion.Lerp(originalCameraRotation, projectCameraRotation, i / loopTimes);

            yield return delay;
        }

        var originalRotation = transform.rotation;
        var destRotation = Quaternion.LookRotation(characterTurnDir);

        for (int i = 0; i < loopTimes; i++)
        {
            transform.rotation = Quaternion.Lerp(originalRotation, destRotation, i / loopTimes);
            yield return delay;
        }
        
        StartCoroutine(PlayEffect(pivot.position + characterTurnDir * 0.2f));
        yield return new WaitForSeconds(0.2f);

        SetModelVisible(false);

        for (int i = 0; i < 20; i++)
        {
            rigidBody.AddForce(-1 * transform.forward * 15);
            yield return null;
        }

        rigidBody.isKinematic = true;
    }

    IEnumerator PlayEffect(Vector3 pos)
    {
        effect.transform.position = pivot.position;
        effect.SetActive(true);
        yield return new WaitForSeconds(0.6f);
        effect.SetActive(false);
    }

    IEnumerator ProjectOnWall()
    {
        rigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        projector.enabled = true;
        
        while (true)
        {
            if (Input.GetKey(KeyCode.Space) == true)
                yield break;

            var dir = Vector3.zero;

            if (Input.GetKey(KeyCode.A) == true)
            {
                projectorAnimation.CrossFade("projectorLeft", 0.1f);
                dir = transform.right;
            }
            
            if (Input.GetKey(KeyCode.D) == true)
            {
                projectorAnimation.CrossFade("projectorRight", 0.1f);
                dir = transform.right * -1;
            }
            
            if (dir == Vector3.zero)
            {
                projector.transform.localRotation = Quaternion.identity;
                projectorAnimation.CrossFade("projectorIdle", 0.1f);
                rigidBody.isKinematic = true;
                yield return null;
                continue;
            }

            rigidBody.isKinematic = false;
            Camera.main.transform.position = pivot.position + transform.forward * (cameraDistance * 2);
            Camera.main.transform.rotation = Quaternion.LookRotation (-1 * transform.forward);

            var movePos = transform.position + dir * Time.fixedDeltaTime * 0.5f;
            //transform.position = movePos;
            rigidBody.position = movePos;
            rigidBody.AddForce(-1 * transform.forward * 5);

            //this function let character can not move at some corner.
            //rigidBody.MovePosition(movePos);

            yield return null;
        }
    }

    GameObject GetTouchWall(ref Vector3 characterTurnDir)
    {
        var results = Physics.OverlapSphere(pivot.position, 0.25f, LayerMask.GetMask("Wall"));
        if (results.Length == 0)
        {
            Debug.Log("Not touch any wall!");
            return null;
        }

        GameObject wall = null;
        Vector3 touchPos = Vector3.zero;

        float nearestDis = 999999;
        for (int i = 0; i < results.Length; i++)
        {
            touchPos = results[i].ClosestPoint(pivot.position);
            var distance = Vector3.Distance(touchPos, pivot.position);
            if (nearestDis >= distance)
            {
                nearestDis = distance;
                wall = results[i].gameObject;
            }
        }

        RaycastHit hitInfo;
        var result = Physics.Raycast(pivot.position, (touchPos - pivot.position), out hitInfo, LayerMask.GetMask("Wall"));
        if (result == false)
            return null;

        characterTurnDir = hitInfo.normal;
        return wall;
    }

    IEnumerator EndProject()
    {
        StartCoroutine(PlayEffect(pivot.position + -1 * transform.forward * 0.2f));
        yield return new WaitForSeconds(0.2f);

        projector.enabled = false;
        transform.rotation = Quaternion.LookRotation(transform.forward);
        SetModelVisible(true);

        var totalTime = 1.0f;
        var deltaTime = 0.01f;

        var delay = new WaitForSeconds(deltaTime);
        var loopTimes = totalTime / deltaTime;

        for (float i = 0; i < loopTimes; i++)
        {
            SetModelLocalZ(Mathf.Lerp(0, 1, i / loopTimes));
            yield return delay;
        }

        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
        rigidBody.isKinematic = false;
    }

    void SetModelVisible(bool bo)
    {
        transform.Find("Belt_02").gameObject.SetActive(bo);
        transform.Find("Pelvis").gameObject.SetActive(bo);
        transform.Find("SpaceMan_Base").gameObject.SetActive(bo);
    }

    void SetModelLocalZ(float z)
    {
        var localScale = transform.localScale;
        localScale.z = z;
        transform.localScale = localScale;
        return;

        var belt = transform.Find("Belt_02").gameObject.transform;
        localScale = belt.localScale;
        localScale.z = z;
        belt.localScale = localScale;

        var pelvis = transform.Find("Pelvis").gameObject.transform;
        localScale = pelvis.localScale;
        localScale.z = z;
        pelvis.localScale = localScale;

        var manBase = transform.Find("SpaceMan_Base").gameObject.transform;
        localScale = manBase.localScale;
        localScale.z = z;
        manBase.localScale = localScale;
    }
}
