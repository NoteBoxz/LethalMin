using UnityEngine;

public class PikminItemOverrideSettings : ScriptableObject
{
    public Item Root = null!;
    public EnemyType EnemyRoot = null!;
    public bool CanBeCarried = true;
    public int PikminNeedOnItem = 1;

}