using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Discord;
using ImGuiNET;
using ImGuiNET.Unity;
using Objects.Items;
using StationeersVR.VRCore.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UI.ImGuiUi;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;
using static ImGuiNET.Unity.CursorShapesAsset;
using static UnityEngine.UIElements.UIR.Allocator2D;

namespace StationeersVR.Utilities
{

    public class SimpleGazeCursor : MonoBehaviour
    {
        public static LineRenderer line;
        //This Enables a line render so you can see the origin and the hit point
        public bool pointerDebug = false;
        //This is the Cursor Sphere that is your cursor while in vr
        public static GameObject cursorInstance;
        //Need Materials so the debug line and the sphere has some color
        public static Material lineMaterial = null;
        public static Material cursorMaterial = null;
        //This is the Scale for the sphere

        public int oldSortingOrder;
        // Use this for initialization
        public void Start()
        {
            if (pointerDebug)
            {
                lineMaterial = new Material(CursorManager.Instance.CursorShader);
                lineMaterial.color = Color.red;
                if (line == null)
                    line = gameObject.AddComponent<LineRenderer>();
                line.material = lineMaterial;
                line.startColor = new Color(0f, 1f, 1f, 1f);
                line.endColor = new Color(1f, 0f, 0f, 1f);
                line.startWidth = 0.002f;
                line.endWidth = 0.004f;
            }
            cursorMaterial = new Material(Shader.Find("Unlit/Color"));
            cursorMaterial.color = Color.white;
            cursorInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cursorInstance.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            cursorInstance.GetComponent<Renderer>().material = cursorMaterial;
            cursorInstance.layer = 30;
            if (cursorInstance.GetComponent<SphereCollider>() != null)
                cursorInstance.GetComponent<SphereCollider>().enabled = false;
            DontDestroyOnLoad(cursorInstance);
        }

        // Update is called once per frame
        public static RaycastResult raycast;
        void Update()
        {
            UpdateCursor();
            if (pointerDebug)
                UpdateDebugPointer();
        }
        //This is where the raycast changes to mouse control and gazecursor
        public static Vector2 GetRayCastMode()
        {
            //Need to use Camera pixelWidth and pixelHeight with the Screen width and heigh so you have no restrction on mouse movement in vr
            if (!InventoryManager.AllowMouseControl)
            {
                return new Vector2(Input.mousePosition.x / Screen.width * Camera.main.pixelWidth, Input.mousePosition.y / Screen.height * Camera.main.pixelHeight);
            }
            else
            {
                return new Vector2(Camera.main.pixelWidth / 2f, Camera.main.pixelHeight / 2f);
            }
        }

        public void ScaleVrCursor(float FixedSize)
        {
            if (Camera.main == null)
            {
                var distance = (Camera.main.transform.position - cursorInstance.transform.position).magnitude;
                var size = distance * FixedSize * Camera.main.fieldOfView;
                cursorInstance.transform.localScale = Vector3.one * size;
                cursorInstance.transform.forward = cursorInstance.transform.position - Camera.main.transform.position;
            }
            else
            {
                var distance = (Camera.main.transform.position - cursorInstance.transform.position).magnitude;
                var size = distance * FixedSize * Camera.main.fieldOfView;
                cursorInstance.transform.localScale = Vector3.one * size;
                cursorInstance.transform.forward = cursorInstance.transform.position - Camera.main.transform.position;
            }
        }

        private void UpdateCursor()
        {
            // Create a gaze ray pointing forward from the camera

            if (Camera.main != null && cursorInstance != null)
            {
                //Scale The Cursor so it's not to small when far and to big when close
                oldSortingOrder = cursorInstance.GetComponent<Renderer>().sortingOrder;
                //This Raycast that hits the UI, Iventory, menus ect.
                if (raycast.gameObject != null && raycast.distance < InputMouse.MaxInteractDistance)
                {
                    ScaleVrCursor(.00008f);
                    if (raycast.gameObject.GetComponentInParent<Canvas>() != null)
                        cursorInstance.GetComponent<Renderer>().sortingOrder = raycast.gameObject.GetComponentInParent<Canvas>().sortingOrder;
                    cursorInstance.transform.position = raycast.worldPosition;
                    cursorMaterial.color = Color.green;
                }
                //This Raycast hits switches, items any interactable but not anything with UI
                else if (CursorManager._raycastHit.transform != null && CursorManager._raycastHit.distance < InputMouse.MaxInteractDistance)
                {
                    ScaleVrCursor(.0002f);
                    cursorInstance.transform.position = CursorManager._raycastHit.point;
                    cursorMaterial.color = Color.green;
                }
                else
                {
                    //These are here so the pointer stays in place or does not disappear when it has no hit point, both gaze and mouse cursor here
                    cursorMaterial.color = Color.white;
                    if (Cursor.lockState == CursorLockMode.None)
                    {
                        cursorInstance.GetComponent<Renderer>().sortingOrder = oldSortingOrder;
                        Vector2 test = new (Input.mousePosition.x / Screen.width * Camera.main.pixelWidth, Input.mousePosition.y / Screen.height * Camera.main.pixelHeight);
                        Vector3 posi = Camera.main.ScreenPointToRay(test).GetPoint(InputMouse.MaxInteractDistance);
                        cursorInstance.transform.position = posi;
                    }
                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        cursorInstance.GetComponent<Renderer>().sortingOrder = oldSortingOrder;
                        Vector3 defaultpos = new  (Camera.main.pixelWidth / 2f, Camera.main.pixelHeight / 2f, InputMouse.MaxInteractDistance);
                        cursorInstance.transform.position = Camera.main.ScreenToWorldPoint(defaultpos);
                    }
                }
            }
        }
        private void UpdateDebugPointer()
        {
            if (Camera.main == null)
            {
                return;
            }
            if (pointerDebug)
            {
                if (raycast.gameObject != null && raycast.distance < InputMouse.MaxInteractDistance)
                {
                    line.SetPosition(1, raycast.worldPosition);
                }
                //This Raycast hits switches,items any interactable but anything with UI
                else if (CursorManager._raycastHit.transform != null && CursorManager._raycastHit.distance < InputMouse.MaxInteractDistance)
                {
                    line.SetPosition(1, CursorManager._raycastHit.point);
                }
                else
                {
                    if (Cursor.lockState == CursorLockMode.None)
                    {
                        Vector2 test = new (Input.mousePosition.x / Screen.width * Camera.main.pixelWidth, Input.mousePosition.y / Screen.height * Camera.main.pixelHeight);
                        Vector3 posi = Camera.main.ScreenPointToRay(test).GetPoint(InputMouse.MaxInteractDistance);
                        line.SetPosition(1, posi);
                    }
                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        Vector3 defaultpos = new (Camera.main.pixelWidth / 2f, Camera.main.pixelHeight / 2f, InputMouse.MaxInteractDistance);
                        line.SetPosition(1, Camera.main.ScreenToWorldPoint(defaultpos));
                    }
                }
            }
        }
    }
}