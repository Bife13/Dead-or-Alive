using UnityEngine;

[CreateAssetMenu(menuName = "Monster Hotel/Monster Definition")]
public class MonsterDefinition : ScriptableObject
{
    public string monsterID;
    public string displayName;
    public int weight;

    public int baseIncome;
    public int stayDuration;

    public EffectType effectType;
    public int effectValue;
    public int effectRequirement;

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
    EmptyAdjacent,
    ExactAdjacency
}
