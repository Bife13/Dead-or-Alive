using UnityEngine;

[CreateAssetMenu(fileName = "New Delays", menuName = "Dead or Alive/Resolution Delays")]
public class ResolutionDelays : ScriptableObject
{
    public float buffDelay;
    public float killDelay;
    public float incomeDelay;
    public float multiplierDelay;
    public float creationDelay;
    public float expiryDelay;
    public float bountyDelay;  
    public float fade;
}
