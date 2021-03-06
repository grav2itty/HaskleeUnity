using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hasklee {

public class CursorN : MonoBehaviour
{
    public float sensitivity = 0.01f;
    public bool alwaysVisible = true;

    public static CursorN Instance { get; private set; }

    [NonSerialized]
    public float dxs, dys;


    private bool directControl = true;
    private bool visible = true;

    private Camera camera;

    private Material mat;
    private MaterialPropertyBlock prop;

    private GameObject lastObject;
    private GameObject draggedObject;

    private GameObject followedObject;
    private Vector3 followOffset;
    private bool viewportFollow;

    private Queue<float> dxq = new Queue<float>();
    private Queue<float> dyq = new Queue<float>();
    private int dqSize = 10;



    public float dx
    {
        get => Input.GetAxis("Mouse X") * sensitivity;
    }

    public float dy
    {
        get => Input.GetAxis("Mouse Y") * sensitivity;
    }

    public bool cursorMoved
    {
        get => !(pos == lastPos);
    }

    public Vector3 pos { get; set; }
    public Vector3 lastPos { get; set; }

    public Vector3 viewportPosition
    {
        get => new Vector3((pos.x+1f)*0.5f, (pos.y+1f)*0.5f, 0);
        set => pos = new Vector3((value.x*2f)-1f, ((value.y*2f)-1f), 0);
    }

    public Vector3 lastViewportPosition
    {
        get => new Vector3((lastPos.x+1f)*0.5f, (lastPos.y+1f)*0.5f, 0);
    }

    private RaycastHit? cursorHit_;
    public RaycastHit? cursorHit
    {
        get => cursorHit_;
    }


    public void FollowObject(GameObject go)
    {
#if HASKLEE_CURSOR
        followedObject = go;
        if (go != null)
        {
            directControl = false;
            visible = false;

            RaycastHit hitInfo;
            if (RayCast(out hitInfo) == true)
            {
                viewportFollow = false;
                followOffset = hitInfo.transform.InverseTransformVector(hitInfo.point - hitInfo.transform.position);

            }
            else
            {
                viewportFollow = true;
                followOffset = viewportPosition - CursorN.Instance.WorldToViewportPoint(go.transform.position);
            }
        }
        else
        {
            directControl = true;
            visible = true;
        }
#endif
    }

    public bool RayCast(out RaycastHit hitInfo)
    {
        Ray ray = camera.ViewportPointToRay(viewportPosition);
        return (Physics.Raycast(ray, out hitInfo));
    }

    public Vector3 WorldToViewportPoint(Vector3 v)
    {
        return camera.WorldToViewportPoint(v);
    }


    private void Draw()
    {
        if (alwaysVisible || visible)
        {
            //why is y negated?
            prop.SetVector("position", new Vector4(pos.x, -pos.y, 0, 1));
            Graphics.DrawProcedural
                (mat, new Bounds(Vector3.zero, new Vector3(100000.0f, 100000.0f, 100000.0f)),
                 MeshTopology.Points, 1, 1, null, prop, ShadowCastingMode.Off, false, 0);
        }
    }

    void Awake()
    {
        Instance = this;
        pos = new Vector3(0,0,0);
        lastPos = new Vector3(0,0,0);
        followedObject = null;

        for (int i=0; i<dqSize; i++)
        {
            dxq.Enqueue(0);
            dyq.Enqueue(0);
        }
        dxs = 0f;

#if HASKLEE_CURSOR
        mat = (Material)Resources.Load("CursorMat", typeof(Material));
        prop = new MaterialPropertyBlock();
#endif
    }

    void Start()
    {
        camera = Camera.main;
#if HASKLEE_CURSOR
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
#endif
    }


#if HASKLEE_CURSOR
    void LateUpdate()
    {
        Draw();
    }
#endif

   void Update()
   {
#if HASKLEE_CURSOR
       if (directControl)
       {
           pos = new Vector3(Math.Min(Math.Max(pos.x + dx, -1), 1),
                             Math.Min(Math.Max(pos.y + dy, -1), 1), pos.z);
       }
       else if (followedObject != null)
       {
           if (viewportFollow == true)
           {
               viewportPosition = WorldToViewportPoint(followedObject.transform.position) + followOffset;
           }
           else
           {
               var offset = followedObject.transform.TransformVector(followOffset);
               viewportPosition = WorldToViewportPoint(followedObject.transform.position + offset);
           }

       }

       RaycastHit hitInfo;
       GameObject currentObject = null;
       Ray ray = camera.ViewportPointToRay(viewportPosition);

       if (Physics.Raycast(ray, out hitInfo))
       {
           cursorHit_ = hitInfo;
           var collider = hitInfo.collider;

           if (collider)
           {
               currentObject = collider.gameObject;
           }
           else
           {
               cursorHit_ = null;
               currentObject = null;
           }
       }

       bool buttonReleased = Input.GetMouseButtonUp(0);
       bool buttonPressed = Input.GetMouseButtonDown(0);
       bool buttonKeptDown = Input.GetMouseButton(0);

       if (currentObject)
       {
           if (currentObject == lastObject)
           {
               currentObject.SendMessage("OnMouseOverN", SendMessageOptions.DontRequireReceiver);
           }
           else
           {
               currentObject.SendMessage("OnMouseEnterN", SendMessageOptions.DontRequireReceiver);
               if (lastObject)
               {
                   lastObject.SendMessage("OnMouseExitN", SendMessageOptions.DontRequireReceiver);
               }
           }

       }
       else if (lastObject)
       {
           lastObject.SendMessage("OnMouseExitN", SendMessageOptions.DontRequireReceiver);
       }

       if (buttonReleased)
       {
           if (currentObject)
           {
               if (currentObject == lastObject)
               {
                   currentObject.SendMessage("OnMouseUpAsButtonN", SendMessageOptions.DontRequireReceiver);
               }
               currentObject.SendMessage("OnMouseUpN", SendMessageOptions.DontRequireReceiver);
           }
           if (draggedObject)
           {
               draggedObject.SendMessage("OnMouseDragEndN", SendMessageOptions.DontRequireReceiver);
               draggedObject = null;
           }
       }
       else if (buttonPressed)
       {
           if (currentObject)
           {
               currentObject.SendMessage("OnMouseDownN", SendMessageOptions.DontRequireReceiver);
           }
       }
       else if (buttonKeptDown)
       {
           if (draggedObject)
           {
               draggedObject.SendMessage("OnMouseDragN", SendMessageOptions.DontRequireReceiver);
           }
           else if (currentObject)
           {
               currentObject.SendMessage("OnMouseDragStartN", SendMessageOptions.DontRequireReceiver);
               draggedObject = currentObject;
           }
       }

       lastObject = currentObject;

#endif
       lastPos = pos;

       dxq.Enqueue(dx);
       dxq.Dequeue();
       dxs = dxq.Average();
       dyq.Enqueue(dy);
       dyq.Dequeue();
       dys = dyq.Average();
   }

}

}
