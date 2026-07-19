using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlsScript : MonoBehaviour
{
   public void Move(InputAction.CallbackContext context)
   {
      Vector2 movementInput = context.ReadValue<Vector2>();
      Debug.Log("Movement Input: " + movementInput);
   }
}
