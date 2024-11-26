using UnityEngine;

[CreateAssetMenu(menuName = "LethalMin/PikminItem OverrideSettings", order = 1)]
public class PikminItemOverrideSettings : ScriptableObject
{
    public Item Root = null!;
    public EnemyType EnemyRoot = null!;
    public bool CanBeCarried = true;
    public int PikminNeedOnItem = 1;

}