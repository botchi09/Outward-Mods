using System;
using System.Reflection;
using UnityEngine;

namespace DebugCameraFix
{
    public class DebugCameraScript : MonoBehaviour
    {
        public bool CameraWasFree = false;
        private int MouseOwnerID;

        public void Initialise()
        {
            On.VideoCamera.Update += new On.VideoCamera.hook_Update(VideoCamera_Update);
        }

        private void VideoCamera_Update(On.VideoCamera.orig_Update orig, VideoCamera self)
        {
            orig(self);

            var m_active = (bool)GetValue(typeof(VideoCamera), self, "m_active");
            var m_character = (Character)GetValue(typeof(VideoCamera), self, "m_character");

            if (m_character)
            {
                if (!m_active)
                {
                    if (MouseOwnerID == -1) { MouseOwnerID = ControlsInput.GetMouseOwner()); }

                    if (CameraWasFree)
                    {
                        CameraWasFree = false;
                        Cursor.lockState = CursorLockMode.Locked;
                        ControlsInput.AssignMouseKeyboardToPlayer(MouseOwnerID);
                    }
                }
                else if (!CameraWasFree)
                {
                    CameraWasFree = true;
                }
            }
        }

        public static void SetValue<T>(T value, Type type, object obj, string field)
        {
            FieldInfo fieldInfo = type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(obj, value);
        }

        public static object GetValue(Type type, object obj, string value)
        {
            FieldInfo fieldInfo = type.GetField(value, BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo.GetValue(obj);
        }
    }
}
