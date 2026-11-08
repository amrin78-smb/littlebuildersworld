using UnityEngine.InputSystem;

namespace LittleBuildersWorld.Input
{
    /// <summary>
    /// Safety net for held controls: if no finger, mouse button or pen is down anywhere, nothing can be held.
    /// This catches lost pointer-up events (notification shade, app switch, OS gesture) so a vehicle never keeps
    /// accelerating on its own.
    /// </summary>
    public static class PointerWatchdog
    {
        public static bool AnyPointerDown()
        {
            Touchscreen ts = Touchscreen.current;
            if (ts != null)
            {
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.isPressed) return true;
                }
            }
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) return true;
            Pen pen = Pen.current;
            if (pen != null && pen.tip.isPressed) return true;
            return false;
        }
    }
}
