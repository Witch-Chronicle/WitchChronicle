using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Drop Table")]
public class DropTable : ScriptableObject
{
    public List<DropEntry> drops = new();
}