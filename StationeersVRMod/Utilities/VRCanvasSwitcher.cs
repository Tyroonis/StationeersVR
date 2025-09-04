using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using BepInEx;
using ImGuiNET;
using StationeersVR.Utilities;
using StationeersVR.VRCore;
using StationeersVR.VRCore.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

namespace StationeersVR.Utilities
{
    [DefaultExecutionOrder(-100)]
    public class VrUiConverter : MonoBehaviour
    {

        public static readonly string[] MainGui =
        [
            "AlertCanvas",
            "CursorCanvas",
            "PanelInputText",
            "PingCanvas",
            "PopupsCanvas",
            "SystemCanvas",
            "GameCanvas",
            "PanelHelpMenu",
            "TooltipCanvas",
            "TraderCanvas",
            "RocketInfoCanvas",
            "FadeCanvas",
            "TutorialNarration",
            "PanelInputCode",
            "ImGui Canvas"
        ];
        [Header("Placement & Head-Movement")]
        public float distanceFromCamera = 2f;
        public float yawThreshold = 0f;

        [Header("World-Space Scale")]
        public float worldScale = 0.002f;

        [Header("CanvasScaler DPI (for ConstantPixelSize)")]
        public float fallbackScreenDPI = 96f;

        private Vector3 _lastCamPos;

        private Transform camTransform;
        private float lastYaw;
        private Vector3 _lastForward;
        private readonly List<Canvas> trackedCanvases = [];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            var go = new GameObject(nameof(VrUiConverter));
            DontDestroyOnLoad(go);
            go.AddComponent<VrUiConverter>();
        }

        void Awake()
        {
            // grab your VR camera (Camera.main by default)
            camTransform = Camera.main?.transform;
            if (camTransform == null)
            {
                Debug.LogError("VRCanvasConverter: No MainCamera found.");
                enabled = false;
                return;
            }
            _lastCamPos = camTransform.position;

            lastYaw = camTransform.eulerAngles.y;
            _lastForward = camTransform.forward;

            // find & convert all overlay/camera canvases
            foreach (var canvas in FindObjectsOfType<Canvas>(true))
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                    canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    ConvertCanvasToWorld(canvas);
                    trackedCanvases.Add(canvas);
                }
            }
        }

        void Update()
        {
            if (camTransform == null) return;

            float currentYaw = camTransform.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(lastYaw, currentYaw);

            if (Mathf.Abs(deltaYaw) >= yawThreshold)
            {
                foreach (var cv in trackedCanvases)
                {
                    ConvertCanvasToWorld(cv);
                    PositionCanvas(cv, camTransform.transform.position.y-1.0f);
                    //PositionCanvas(cv);
                }

                lastYaw = currentYaw;
            }
        }
        
        private void ConvertCanvasToWorld(Canvas canvas)
        {
            // switch to WorldSpace
            if (canvas.name == "Canvas" && SceneManager.GetActiveScene().name != "Splash") return;
            if (SceneManager.GetActiveScene().name == "Splash")
                worldScale = 0.02f; // splash screen is smaller
            else
                worldScale = 0.002f; // default world scale
            foreach (var t in canvas.GetComponentsInChildren<Canvas>(true))
            {
                t.gameObject.layer = 30;
                // ModLog.Error("Canvas: " + t.gameObject.layer.name);
            }

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.gameObject.layer = 30;
            
            // ensure root rect matches screen size in pixels
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(Screen.width, Screen.height);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // add a ConstantPixelSize scaler so no UI squish occurs
            var scaler = canvas.GetComponent<CanvasScaler>()
                         ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;
            scaler.fallbackScreenDPI = fallbackScreenDPI;

            // apply a uniform world-scale
            var scale = new Vector3(0.5f, 0.5f, 0.5f);
            rt.localScale = scale * worldScale;
            // initial placement in front of camera
            PositionCanvas(canvas, camTransform.transform.position.y-1.0f);
        }


        private void PositionCanvas(Canvas canvas, float lockedY)
        {
            // 1) Slide side-to-side exactly as before
            Vector3 camDelta = camTransform.position - _lastCamPos;
            Vector3 lateralOffset = Vector3.Project(camDelta, camTransform.right);

            // 2) Yaw-threshold snap for _lastForward_ (your existing logic)
            float currentYaw = camTransform.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(lastYaw, currentYaw);
            if (Mathf.Abs(deltaYaw) >= yawThreshold)
            {
                lastYaw = currentYaw;
                _lastForward = camTransform.forward;
            }

            // 3) Recompute world position at lockedY
            Vector3 forwardAnchor = camTransform.position + _lastForward * distanceFromCamera;
            Vector3 newPos = forwardAnchor + lateralOffset;
            newPos.y = lockedY;
            canvas.transform.position = newPos;
            //canvas.transform.position = newPos;

            // 4) Full-3D billboard parent canvas to camera
            //    This handles pitch+yaw+roll perfectly
            canvas.transform.LookAt(camTransform, Vector3.up);
            //    Now flip 180° around Y so the front of the canvas faces the camera
            canvas.transform.Rotate(0f, 180f, 0f, Space.Self);

            // 5) Do the exact same for every child
            foreach (Transform child in canvas.transform)
            {
                child.LookAt(camTransform, Vector3.up);
                child.Rotate(0f, 180f, 0f, Space.Self);
            }

            // 6) Cache camera pos for next frame
            _lastCamPos = camTransform.position;
        }
    }
}
