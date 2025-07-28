using UnityEngine;
using System.Collections.Generic;
using MelonLoader;

namespace BankOrganizer.Camera
{
    /// <summary>
    /// Manages blocking and unblocking of camera movement and controls
    /// </summary>
    public static class CameraBlocker
    {
        private static bool _isBlocking = false;
        private static List<MonoBehaviour> _disabledScripts = new List<MonoBehaviour>();
        private static List<UnityEngine.Camera> _disabledCameras = new List<UnityEngine.Camera>();

        /// <summary>
        /// Gets whether camera blocking is currently active
        /// </summary>
        public static bool IsBlocking => _isBlocking;

        /// <summary>
        /// Enable camera blocking - disables camera movement, rotation, and zoom
        /// </summary>
        public static void EnableBlocking()
        {
            if (_isBlocking) return;

            _isBlocking = true;
            _disabledScripts.Clear();
            _disabledCameras.Clear();

            try
            {
                // Find and disable common camera controller script types
                DisableCameraControllers();

                // Disable input processing for cameras
                DisableCameraInputs();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Error enabling camera blocking: {ex.Message}");
            }
        }

        /// <summary>
        /// Disable camera blocking - re-enables camera movement, rotation, and zoom
        /// </summary>
        public static void DisableBlocking()
        {
            if (!_isBlocking) return;

            _isBlocking = false;

            try
            {
                // Re-enable all disabled scripts
                foreach (var script in _disabledScripts)
                {
                    if (script != null)
                    {
                        script.enabled = true;
                    }
                }

                // Re-enable all disabled cameras
                foreach (var camera in _disabledCameras)
                {
                    if (camera != null)
                    {
                        camera.enabled = true;
                    }
                }

                _disabledScripts.Clear();
                _disabledCameras.Clear();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Error disabling camera blocking: {ex.Message}");
            }
        }

        private static void DisableCameraControllers()
        {
            // Find all cameras in the scene
            UnityEngine.Camera[] allCameras = UnityEngine.Object.FindObjectsOfType<UnityEngine.Camera>();

            foreach (UnityEngine.Camera cam in allCameras)
            {
                if (cam == null || cam.gameObject == null) continue;

                // Get all MonoBehaviour components on the camera GameObject
                MonoBehaviour[] scripts = cam.gameObject.GetComponents<MonoBehaviour>();

                foreach (MonoBehaviour script in scripts)
                {
                    if (script == null) continue;

                    // Check if this looks like a camera controller script
                    if (IsCameraController(script))
                    {
                        if (script.enabled)
                        {
                            script.enabled = false;
                            _disabledScripts.Add(script);
                        }
                    }
                }

                // Also check parent objects for camera controllers
                Transform parent = cam.transform.parent;
                if (parent != null)
                {
                    MonoBehaviour[] parentScripts = parent.gameObject.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour script in parentScripts)
                    {
                        if (script == null) continue;

                        if (IsCameraController(script))
                        {
                            if (script.enabled)
                            {
                                script.enabled = false;
                                _disabledScripts.Add(script);
                            }
                        }
                    }
                }
            }
        }

        private static void DisableCameraInputs()
        {
            // Look for any objects with camera-related input scripts
            // This is a broader search for any scripts that might control camera movement
            MonoBehaviour[] allScripts = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();

            foreach (MonoBehaviour script in allScripts)
            {
                if (script == null) continue;

                if (IsCameraInputController(script))
                {
                    if (script.enabled)
                    {
                        script.enabled = false;
                        _disabledScripts.Add(script);
                    }
                }
            }
        }

        private static bool IsCameraController(MonoBehaviour script)
        {
            if (script == null) return false;

            string typeName = script.GetType().Name.ToLower();
            string gameObjectName = script.gameObject.name.ToLower();

            // Common camera controller script names
            return typeName.Contains("camera") ||
                   typeName.Contains("orbit") ||
                   typeName.Contains("freelook") ||
                   typeName.Contains("mouselook") ||
                   typeName.Contains("fpscamera") ||
                   typeName.Contains("thirdperson") ||
                   typeName.Contains("zoom") ||
                   typeName.Contains("pan") ||
                   typeName.Contains("rotate") ||
                   gameObjectName.Contains("camera");
        }

        private static bool IsCameraInputController(MonoBehaviour script)
        {
            if (script == null) return false;

            string typeName = script.GetType().Name.ToLower();

            // Look for input controllers that might affect camera movement
            return (typeName.Contains("input") && typeName.Contains("camera")) ||
                   (typeName.Contains("mouse") && (typeName.Contains("look") || typeName.Contains("control"))) ||
                   typeName.Contains("cameracontrol") ||
                   typeName.Contains("camerainput");
        }

        /// <summary>
        /// Force disable all camera-related components (more aggressive approach)
        /// </summary>
        public static void ForceDisableAllCameras()
        {
            if (!_isBlocking) return;

            try
            {
                // Get all cameras and disable them entirely
                UnityEngine.Camera[] allCameras = UnityEngine.Object.FindObjectsOfType<UnityEngine.Camera>();

                foreach (UnityEngine.Camera cam in allCameras)
                {
                    // Skip UI cameras or cameras that shouldn't be disabled
                    if (cam.name.ToLower().Contains("ui") ||
                        cam.name.ToLower().Contains("overlay") ||
                        cam.cullingMask == 32) // UI layer
                        continue;

                    if (cam.enabled)
                    {
                        cam.enabled = false;
                        _disabledCameras.Add(cam);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Error force disabling cameras: {ex.Message}");
            }
        }

        /// <summary>
        /// Get status information about currently blocked components
        /// </summary>
        public static string GetBlockingStatus()
        {
            if (!_isBlocking)
                return "Camera blocking: OFF";

            return $"Camera blocking: ON - {_disabledScripts.Count} scripts disabled, {_disabledCameras.Count} cameras disabled";
        }
    }
}