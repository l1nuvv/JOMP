using StarterAssets;
using UnityEngine;

namespace JustOneMorePeel
{
    internal sealed class PeelScreenController
    {
        FirstPersonController controller;
        StarterAssetsInputs inputs;
        bool lookFrozen;
        internal bool ApplicationFocused { get; set; }=true;
        internal PeelScreen CurrentScreen { get; set; }=PeelScreen.Intro;

        internal void Initialize(FirstPersonController controller,StarterAssetsInputs inputs)
        {
            this.controller=controller;this.inputs=inputs;
        }

        internal void SetLookFrozen(bool frozen)
        {
            if(inputs && lookFrozen!=frozen)inputs.look=Vector2.zero;
            lookFrozen=frozen;RefreshCursor();
        }

        internal void ClearMovement()
        {
            if(inputs){inputs.move=Vector2.zero;inputs.jump=false;inputs.sprint=false;}
        }

        internal void RefreshCursor()
        {
            bool playing=ApplicationFocused && CurrentScreen==PeelScreen.Playing;
            if(inputs)
            {
                inputs.cursorLocked=playing;inputs.cursorInputForLook=playing && !lookFrozen;
                if(!playing || lookFrozen)inputs.look=Vector2.zero;
            }
            Cursor.lockState=playing?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!playing;
            if(controller)controller.enabled=playing && !lookFrozen;
        }
        internal void LateUpdate()
        {
            if(CurrentScreen!=PeelScreen.Playing && (Cursor.lockState!=CursorLockMode.None || !Cursor.visible))RefreshCursor();
        }
    }
}
