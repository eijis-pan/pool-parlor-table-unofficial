
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ButtonRequestBreak : UdonSharpBehaviour
{
    [SerializeField] BilliardsModule table;
    public uint teamId;

    public override void Interact()
    {
        table._RequestBreak(teamId);
    }
}
