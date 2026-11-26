using UnityEngine;

// Controle de exploração em terceira pessoa: teclado/mouse no PC e toque no Android.
[RequireComponent(typeof(CharacterController))]
public class MedievalThirdPersonController : MonoBehaviour
{
    public float walkSpeed=4.2f, runSpeed=6.5f, gravity=-22f, jumpHeight=1.25f;
    public Transform cameraPivot;
    private CharacterController cc;
    private Transform cam;
    private float vertical, yaw, pitch=12f;
    private Vector2 touchMove, lookDelta;
    private int moveFinger=-1, lookFinger=-1;
    private bool runHeld, jumpQueued;
    private Vector2 moveStart, lookLast;

    void Awake()
    {
        cc=GetComponent<CharacterController>();
        cam=GetComponentInChildren<Camera>()?.transform;
        if(cam!=null){cameraPivot=cam.parent; yaw=transform.eulerAngles.y;}
        Cursor.lockState=CursorLockMode.None;
    }

    void Update()
    {
        Vector2 move=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
        bool sprint=Input.GetKey(KeyCode.LeftShift)||runHeld;
        for(int i=0;i<Input.touchCount;i++)
        {
            Touch t=Input.GetTouch(i); Vector2 pos=t.position;
            if(t.phase==TouchPhase.Began)
            {
                if(pos.x<Screen.width*.48f && moveFinger==-1){moveFinger=t.fingerId;moveStart=pos;}
                else if(pos.x>=Screen.width*.48f && lookFinger==-1){lookFinger=t.fingerId;lookLast=pos;}
            }
            if(t.fingerId==moveFinger)
            {
                if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled){moveFinger=-1;touchMove=Vector2.zero;}
                else {Vector2 delta=(pos-moveStart)/75f;touchMove=Vector2.ClampMagnitude(delta,1f);}
            }
            if(t.fingerId==lookFinger)
            {
                if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled){lookFinger=-1;}
                else {lookDelta=(pos-lookLast)*.12f;lookLast=pos;}
            }
        }
        if(moveFinger!=-1)move=touchMove;
        if(Input.GetMouseButton(1)){yaw+=Input.GetAxis("Mouse X")*3f;pitch-=Input.GetAxis("Mouse Y")*2f;}
        yaw+=lookDelta.x; pitch-=lookDelta.y; lookDelta=Vector2.zero; pitch=Mathf.Clamp(pitch,-8f,48f);
        if(cameraPivot!=null)cameraPivot.rotation=Quaternion.Euler(pitch,yaw,0);
        Vector3 forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
        Vector3 right=Quaternion.Euler(0,yaw,0)*Vector3.right;
        Vector3 direction=Vector3.ClampMagnitude(forward*move.y+right*move.x,1f);
        if(direction.sqrMagnitude>.01f)
        {
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),12f*Time.deltaTime);
        }
        if(cc.isGrounded && vertical<0)vertical=-2f;
        if(cc.isGrounded && (Input.GetKeyDown(KeyCode.Space)||jumpQueued))
            vertical=Mathf.Sqrt(jumpHeight*-2f*gravity);
        jumpQueued=false; vertical+=gravity*Time.deltaTime;
        cc.Move((direction*(sprint?runSpeed:walkSpeed)+Vector3.up*vertical)*Time.deltaTime);
    }

    void OnGUI()
    {
        if(Application.isMobilePlatform)
        {
            float w=Screen.width,h=Screen.height;
            GUI.color=new Color(1,1,1,.75f);
            if(GUI.RepeatButton(new Rect(w-145,h-112,62,62),"CORRER"))runHeld=true;else runHeld=false;
            if(GUI.Button(new Rect(w-75,h-112,62,62),"PULAR"))jumpQueued=true;
            GUI.color=Color.white;
            GUI.Label(new Rect(18,h-32,280,24),"Arraste à esquerda para andar · à direita para olhar");
        }
    }
}
