using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

public class InputClass : LcInputActions
{
    [InputAction(KeyboardControl.Num4, Name = "Throw")]
    public InputAction Throw { get; set; }

    
    [InputAction(MouseControl.LeftButton, Name = "Whistle")]
    public InputAction Whistle { get; set; }


    [InputAction(MouseControl.MiddleButton, Name = "Dismiss")]
    public InputAction Dismiss { get; set; }


    [InputAction(KeyboardControl.Num2, Name = "SwitchL")]
    public InputAction SwitchLeft { get; set; }

    
    [InputAction(KeyboardControl.Num3, Name = "SwitchR")]
    public InputAction SwitchRight { get; set; }
}