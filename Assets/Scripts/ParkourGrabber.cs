using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkourGrabber : MonoBehaviour
{
    public ObstacleChecker obstCheck;
    public SphereCollider topColl;
    public CapsuleCollider lowColl;

    [SerializeField] private HandsAnimator handAnim;

    private FPController fpController;
    private Rigidbody rb;

    private float defClimbspeed;
    public float climbSpeed =0.5f;
    private bool canWallJump;
    [SerializeField]private bool doingMove = false;

    private void Start()
    {
        fpController = GetComponent<FPController>();
        rb = GetComponent<Rigidbody>();
        defClimbspeed = climbSpeed;
    }
    void Update()
    {
        
        //if unput vaultover & nothin g in the way & not doing another move //&& has speed forward
        if (Input.GetButton("Hand") && obstCheck.blockedLow &! obstCheck.blockedForward &! obstCheck.blockedMid &! doingMove)
        {
            if (Input.GetAxisRaw("VerticalForward")>0)
            {
                doingMove = true;
                //pause grav, disable lower collider, timer
                VaultForward();
            }
        }
        //jump & obstacleinfront+low & space above it & not doing another
        else if (Input.GetButton("Jump") && obstCheck.blockedLow & !obstCheck.blockedMid & !obstCheck.blockedAbove & !doingMove)
        {
            if (Input.GetAxisRaw("VerticalForward") > 0)
            {
                fpController.StopJump();
                doingMove = true;
                VaultUp();
            }
        }
        //jump & obstacleinfront & space above it & not doing another
        else if (Input.GetButton("Jump") && obstCheck.blockedMid &!obstCheck.blockedAbove & !doingMove)
        {
            if (Input.GetAxisRaw("VerticalForward") > 0)
            {
                fpController.StopJump();
                doingMove = true;
                ClimbUp();
            }
            
        }
        else if(Input.GetButtonDown("Jump") & !doingMove && canWallJump)
        {
            WallJump((fpController.moveRaw*2+Vector3.up));
        }
        else if(Input.GetButtonDown("Slide") &!doingMove)
        {
            if (Input.GetAxisRaw("VerticalForward") > 0 && fpController.grounded)
                Slide();
        }
    }
    public void VaultForward()
    {
        //play vault1 animation //TODO make several and randomize
        handAnim.SetTrigger("Vault1");

        //lerping legs collider and gravityscale for climbeffect
        lowColl.center = new Vector3(0, 0.4f, 0);
        IEnumerator delayHolder = LerpColliderCenter(new Vector3(0, -0.7f, 0), 0.25f, lowColl);
        StartCoroutine(CoroutineDelay(delayHolder,0.25f));
        fpController.gravityScale = fpController.gravityScale/50;
        fpController.Invoke("ResetGravityScale",0.25f);

        //automove
        fpController.StartCoroutine(fpController.LerpAutoMove(fpController.moveRaw*3, 0.25f));

        //reset doingMove in 500ms
        StartCoroutine(VarChange(result => doingMove = result, 0.5f, false));
        //lower sensmultiplier during move
        fpController.sensMultiplier = 0.5f;
        StartCoroutine(VarChange(result => fpController.sensMultiplier = result, 0.5f, 1f));
    }
    public void VaultUp()
    {
        //play vault1 animation //TODO make several and randomize
        handAnim.SetTrigger("VaultUp1");

        //lerping legs collider and gravityscale for climbeffect
        lowColl.center = new Vector3(0, 0.4f, 0);
        IEnumerator delayHolder = LerpColliderCenter(new Vector3(0, -0.7f, 0), 0.25f, lowColl);
        StartCoroutine(CoroutineDelay(delayHolder, 0.1f));
        fpController.gravityScale = fpController.gravityScale / 50;
        fpController.Invoke("ResetGravityScale", 0.75f);

        //automove
        fpController.StartCoroutine(fpController.LerpAutoMove(fpController.moveRaw * 2, 0.25f));

        //lerpY

        //ray for destination point
        RaycastHit ray;
            Vector3 raystartpos = new Vector3(obstCheck.transform.position.x, obstCheck.transform.position.y + 2, obstCheck.transform.position.z);
            Physics.Raycast(raystartpos, Vector3.down, out ray, 2f);
            Vector3 pos = ray.point + Vector3.up*0.5f; //y+1 cus player center is higher

        //automove Y
        fpController.StartCoroutine(fpController.LerpPlayerY(pos.y, 0.25f));

        //reset doingMove in 500ms
        StartCoroutine(VarChange(result => doingMove = result, 0.5f, false));
        //sensmultiplier during move
        fpController.sensMultiplier = 0.5f;
        StartCoroutine(VarChange(result => fpController.sensMultiplier = result, 0.5f, 1f));
    }
    public void ClimbUp()
    {

        //anim
        handAnim.SetTrigger("Climb1");
        //match speed of climb with anim
        handAnim.SetAnimSpeed(Mathf.Pow(climbSpeed,-1));

        //lerppos
        //ray for destination point
        RaycastHit ray;
            Vector3 raystartpos = new Vector3(obstCheck.transform.position.x, obstCheck.transform.position.y + 5, obstCheck.transform.position.z);
            Physics.Raycast(raystartpos, Vector3.down, out ray, 5f);
        Vector3 pos = ray.point - Vector3.up; //centeroffset
        climbSpeed = pos.y - transform.position.y - 1.5f;
            
        //extra forwardmove so you wont fall bdown backwards
        float xzDif = (pos.x + pos.z) - (transform.position.x + transform.position.z);
        if (xzDif < 1.2f)
            pos += transform.forward * 0.25f;

        //climbingspeed
        float cSpeed = Vector3.Distance(ray.point, Camera.main.transform.position);
        fpController.StartCoroutine(fpController.SlerpPlayerPos(pos, climbSpeed));
        //fpController.StartCoroutine(fpController.LerpPlayerY(pos.y,climbSpeed));


        //lowcolLerpfor fallthru-proofing //TODO convert all to method
        lowColl.center = new Vector3(0, 0.4f, 0);
        IEnumerator delayHolder = LerpColliderCenter(new Vector3(0, -0.7f, 0), climbSpeed/4, lowColl);
        StartCoroutine(CoroutineDelay(delayHolder, climbSpeed/2));

        //tempdisable colliders for posLerp
        topColl.enabled = false;
        lowColl.enabled = false;
        StartCoroutine(VarChange(result => topColl.enabled = result, climbSpeed-0.25f, true));
        StartCoroutine(VarChange(result => lowColl.enabled = result, climbSpeed-0.25f, true));

        //reset doingMove in 500ms
        StartCoroutine(VarChange(result => doingMove = result, 1f, false));
        //sensmultiplier
        fpController.sensMultiplier = 0.5f;
        StartCoroutine(VarChange(result => fpController.sensMultiplier = result, 0.5f, 1f));
    }
    public void WallJump(Vector3 dir)
    {
        fpController.StartCoroutine(fpController.LerpAutoMove(dir, 0.15f));
        fpController.gravityScale = 0;
        fpController.Invoke("ResetGravityScale", 0.15f);
    }
    public void Slide()
    {
        //bonus grav
        fpController.gravityScale = fpController.gravityScale * 50;
        fpController.Invoke("ResetGravityScale", 0.15f);

        //lerping legs collider and for raise effect
        lowColl.center = new Vector3(0, 0.4f, 0);
        IEnumerator delayHolder = LerpColliderCenter(new Vector3(0, -0.7f, 0), 0.25f, lowColl);
        StartCoroutine(CoroutineDelay(delayHolder, 1f));

        //automove
        fpController.StartCoroutine(fpController.LerpAutoMove(fpController.moveRaw, 0.25f));
    }
    public void MoveDone()
    {
        doingMove = false;
    }
    public void CanWallJump(bool canWallJump)
    {
        this.canWallJump = canWallJump;
        fpController.gravityScale = fpController.gravityScale / 25;
        fpController.Invoke("ResetGravityScale", 0.5f);

    }
    //changes bool after cooldown
    IEnumerator VarChange(System.Action<bool> boolVar, float cooldown, bool endValue)
    {
        yield return new WaitForSeconds(cooldown);
        boolVar(endValue);
    }
    //change float after cooldown
    IEnumerator VarChange(System.Action<float> floatVar, float cooldown, float endValue)
    {
        yield return new WaitForSeconds(cooldown);
        floatVar(endValue);
    }
    //lerp centervalue of capsule collider
    private IEnumerator LerpColliderCenter(Vector3 endVal, float duration, CapsuleCollider capsuleCollider)
    {
        float time = 0;
        Vector3 startVal = capsuleCollider.center;
        while (time < duration)
        {
            capsuleCollider.center = Vector3.Lerp(startVal, endVal, time/duration);
            time += Time.deltaTime;
            yield return null;
        }
        capsuleCollider.center = endVal;
    }
    //starts another coroutine after delay
    private IEnumerator CoroutineDelay(IEnumerator ien, float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(ien);
    }
}
