using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

public class InputClassWithUtils : LcInputActions
{
    [InputAction(KeyboardControl.R, Name = "Throw")]
    public InputAction Throw { get; set; } = null!;
    [InputAction(KeyboardControl.Q, Name = "ThrowCancel")]
    public InputAction ThrowCancel { get; set; } = null!;

    [InputAction(KeyboardControl.Num4, Name = "Charge")]
    public InputAction Charge { get; set; } = null!;

    [InputAction(MouseControl.LeftButton, Name = "Whistle(Item)")]
    public InputAction Whistle { get; set; } = null!;

    [InputAction(MouseControl.MiddleButton, Name = "Dismiss")]
    public InputAction Dismiss { get; set; } = null!;

    [InputAction(KeyboardControl.RightBracket, Name = "SwitchWhistleSound")]
    public InputAction SwitchWhistleAud { get; set; } = null!;

    [InputAction(KeyboardControl.Num2, Name = "SwitchL")]
    public InputAction SwitchLeft { get; set; } = null!;

    [InputAction(KeyboardControl.Num3, Name = "SwitchR")]
    public InputAction SwitchRight { get; set; } = null!;

    [InputAction(KeyboardControl.Num4, Name = "Glowmob")]
    public InputAction Glowmob { get; set; } = null!;

    [InputAction(KeyboardControl.Shift, Name = "OnionHudSpeed")]
    public InputAction OnionHudSpeed { get; set; } = null!; 
}