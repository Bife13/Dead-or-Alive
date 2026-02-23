using UnityEngine;

[CreateAssetMenu(menuName = "Monster Hotel/Monster Definition")]
public class MonsterDefinition : ScriptableObject
{
    public string monsterID;
    public string displayName;

    public int baseIncome;

    public EffectType effectType;
    public int effectValue;

    public MonsterDefinition summonDefinition;

    public bool isTemporary;
    public bool canCopy;

    public GameObject monsterPrefab;
}

public enum EffectType
{
    None,
    BuffAdjacentFlat,
    SummonAdjacent,
    KillAdjacent,
    MoveTowards,
    ConditionalMultNoDeaths,
    FlatMultAlways,
}
