using System;
using FABSim.Core;
using UnityEngine.InputSystem;

namespace FABSim.Game
{
    public sealed class FabInput
    {
        double kbPitch, kbRoll, kbYaw, padYaw, holdVal;
        bool hold, padThr, speedbrakeOn;
        public bool Invert;

        public bool ToggleCamera, ToggleMenu, ToggleMap, Restart, NextWeapon, PrevWeapon, Fire, FireHeld, Gun, Flare, NextTarget;
        public bool ToggleProbe, ToggleAp, ToggleLight, ToggleArm, Jettison, NextPage, LookZoomed;
        public double ZoomAxis;
        bool modUsado;
        public double LookYaw, LookPitch;
        public bool ThrottleLocked => hold;

        static double Clamp(double x, double a, double b) => x < a ? a : x > b ? b : x;

        static double Shape(double v)
        {
            double a = Math.Abs(v);
            if (a < 0.05) return 0;
            double n = Math.Min(1, (a - 0.05) / (0.94 - 0.05));
            return Math.Sign(v) * (0.7 * n + 0.3 * n * n * n);
        }

        static double Ramp(double cur, double target, double dt, double cap = 1, double up = 1.3, double down = 3)
        {
            double rate = target == 0 ? down : up;
            return cur + Clamp(target * cap - cur, -rate * dt, rate * dt);
        }

        public void Read(Controls c, FlightState s, AircraftDef ac, double dt)
        {
            ToggleCamera = ToggleMenu = ToggleMap = Restart = NextWeapon = PrevWeapon = Fire = Flare = NextTarget = false;
            ToggleProbe = ToggleAp = ToggleLight = ToggleArm = Jettison = NextPage = false;
            LookYaw = LookPitch = ZoomAxis = 0;
            var mouse = Mouse.current;
            if (mouse != null && mouse.scroll.y.ReadValue() != 0) ZoomAxis = Math.Sign(mouse.scroll.y.ReadValue()) * 8;
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            bool air = !s.Contact;

            double K(bool on) => on ? 1 : 0;
            kbPitch = Ramp(kbPitch, kb == null ? 0 : K(kb.downArrowKey.isPressed) - K(kb.upArrowKey.isPressed), dt, 0.85, 2.6, 5);
            kbRoll = Ramp(kbRoll, kb == null ? 0 : K(kb.rightArrowKey.isPressed) - K(kb.leftArrowKey.isPressed), dt, 0.8, 2.4, 5);
            kbYaw = Ramp(kbYaw, kb == null ? 0 : K(kb.dKey.isPressed) - K(kb.aKey.isPressed), dt, 0.8, 2.4, 5);
            double pitch = kbPitch, roll = kbRoll, yaw = kbYaw;
            double throttleD = 0, trimD = 0, brake = 0;
            bool gun = false, fireHeld = false;

            if (kb != null)
            {
                throttleD = K(kb.wKey.isPressed) - K(kb.sKey.isPressed);
                trimD = K(kb.eKey.isPressed) - K(kb.qKey.isPressed);
                if (air) gun = kb.spaceKey.isPressed; else brake = K(kb.spaceKey.isPressed);
                fireHeld = kb.fKey.isPressed || kb.enterKey.isPressed;
                Fire |= kb.fKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                Flare |= kb.lKey.wasPressedThisFrame;
                NextTarget |= kb.tKey.wasPressedThisFrame;
                NextWeapon |= kb.tabKey.wasPressedThisFrame;
                ToggleCamera |= kb.vKey.wasPressedThisFrame;
                ToggleMenu |= kb.escapeKey.wasPressedThisFrame;
                ToggleMap |= kb.nKey.wasPressedThisFrame;
                Restart |= kb.rKey.wasPressedThisFrame;
                if (kb.gKey.wasPressedThisFrame) c.GearDown = !c.GearDown;
                if (kb.pKey.wasPressedThisFrame) c.Parking = !c.Parking;
                if (kb.bKey.wasPressedThisFrame) speedbrakeOn = !speedbrakeOn;
                if (kb.zKey.wasPressedThisFrame) c.FlapIdx = Math.Max(0, c.FlapIdx - 1);
                if (kb.xKey.wasPressedThisFrame) c.FlapIdx = Math.Min(ac.FlapNotches.Length - 1, c.FlapIdx + 1);
                ToggleProbe |= kb.jKey.wasPressedThisFrame;
                ToggleAp |= kb.uKey.wasPressedThisFrame;
                ToggleLight |= kb.hKey.wasPressedThisFrame;
                ToggleArm |= kb.kKey.wasPressedThisFrame;
                Jettison |= kb.deleteKey.wasPressedThisFrame;
                NextPage |= kb.mKey.wasPressedThisFrame;
                LookYaw = K(kb.numpad6Key.isPressed) - K(kb.numpad4Key.isPressed);
                LookPitch = K(kb.numpad8Key.isPressed) - K(kb.numpad2Key.isPressed);
                double zoomKb = K(kb.pageUpKey.isPressed) - K(kb.pageDownKey.isPressed);
                if (zoomKb != 0) ZoomAxis = zoomKb;
            }
            if (throttleD != 0) padThr = false;
            double thr = Clamp(c.Throttle + throttleD * 0.35 * dt, 0, 1);

            if (pad != null)
            {
                double px = Shape(-pad.leftStick.y.ReadValue()), rx = Shape(pad.leftStick.x.ReadValue());
                if (px != 0 || rx != 0) { pitch = px; roll = rx; }
                double lx = Shape(pad.rightStick.x.ReadValue()), ly = Shape(pad.rightStick.y.ReadValue());
                if (lx != 0) LookYaw = lx;
                // Frente e tras aproximam; com View segurado o direcional olha para cima e para baixo.
                if (ly != 0)
                {
                    if (pad.selectButton.isPressed) { LookPitch = ly; modUsado = true; }
                    else ZoomAxis = ly;
                }
                padYaw = Ramp(padYaw, pad.rightShoulder.ReadValue() - pad.leftShoulder.ReadValue(), dt, 1, 2.5, 4);
                yaw = Clamp(yaw + padYaw, -1, 1);
                double rt = pad.rightTrigger.ReadValue();
                if (rt > 0.03 || hold) padThr = true;
                if (padThr) thr = Math.Max(rt, hold ? holdVal : 0);

                if (air)
                {
                    gun |= pad.leftTrigger.ReadValue() > 0.3f;
                    fireHeld |= pad.buttonEast.isPressed;
                    Fire |= pad.buttonEast.wasPressedThisFrame;
                }
                else brake = Math.Max(brake, pad.buttonEast.ReadValue());
                // View segurado funciona como modificador; um toque curto continua sendo o aerofreio.
                bool mod = pad.selectButton.isPressed;
                if (pad.selectButton.wasPressedThisFrame) modUsado = false;
                if (mod)
                {
                    double trimPad = K(pad.dpad.up.isPressed) - K(pad.dpad.down.isPressed);
                    if (trimPad != 0) { trimD = trimPad; modUsado = true; }
                    if (pad.dpad.left.wasPressedThisFrame) { ToggleProbe = true; modUsado = true; }
                    if (pad.dpad.right.wasPressedThisFrame) { ToggleLight = true; modUsado = true; }
                    if (pad.buttonNorth.wasPressedThisFrame) { ToggleAp = true; modUsado = true; }
                    if (pad.buttonWest.wasPressedThisFrame) { ToggleArm = true; modUsado = true; }
                    if (pad.buttonSouth.wasPressedThisFrame) { Jettison = true; modUsado = true; }
                }
                else
                {
                    Flare |= pad.buttonWest.wasPressedThisFrame;
                    NextTarget |= pad.buttonNorth.wasPressedThisFrame;
                    NextWeapon |= pad.dpad.down.wasPressedThisFrame;
                    PrevWeapon |= pad.dpad.up.wasPressedThisFrame;
                    ToggleMap |= pad.dpad.left.wasPressedThisFrame;
                    if (pad.buttonSouth.wasPressedThisFrame) c.GearDown = !c.GearDown;
                    if (pad.dpad.right.wasPressedThisFrame) c.FlapIdx = (c.FlapIdx + 1) % ac.FlapNotches.Length;
                }
                if (pad.selectButton.wasReleasedThisFrame && !modUsado) speedbrakeOn = !speedbrakeOn;
                ToggleCamera |= pad.leftStickButton.wasPressedThisFrame;
                ToggleMenu |= pad.startButton.wasPressedThisFrame;
                if (pad.rightStickButton.wasPressedThisFrame)
                {
                    if (LookZoomed) NextPage = true;
                    else { hold = !hold; holdVal = c.Throttle; }
                }
            }

            Gun = gun; FireHeld = fireHeld;
            c.Pitch = Clamp(pitch * (Invert ? -1 : 1), -1, 1);
            c.Roll = Clamp(roll, -1, 1);
            c.Yaw = Clamp(yaw, -1, 1);
            if (c.Parking && thr > 0.25) c.Parking = false;
            c.Throttle = thr;
            c.Brake = brake;
            c.Speedbrake = speedbrakeOn ? 1 : 0;
            c.Trim = Clamp(c.Trim + trimD * 0.15 * dt, -1, 1);
        }
    }
}
