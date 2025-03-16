
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ButtonPushOut : UdonSharpBehaviour
{
    [SerializeField] BilliardsModule table;

    public override void Interact()
    {
        table._PushOut();
    }
}
