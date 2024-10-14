
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ButtonCallShotLock : UdonSharpBehaviour
{
    [SerializeField] BilliardsModule table;

    public override void Interact()
    {
        table._CallShotLock();
    }
}
