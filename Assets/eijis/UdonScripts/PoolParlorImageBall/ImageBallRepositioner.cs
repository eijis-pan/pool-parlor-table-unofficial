using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ImageBallRepositioner : UdonSharpBehaviour
{
    [NonSerialized] public int idx;

    private ImageBallManager manager;
    private BilliardsModule table;
    private VRC_Pickup pickup;

    public void _Init(ImageBallManager manager, int idx_)
    {
        this.manager = manager;
        table = manager.table;
        idx = idx_;
        
        pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
    }

    public override void OnPickup()
    {
        manager._BeginReposition(this);
    }

    public override void OnDrop()
    {
        manager._EndReposition(this);
    }

    public void _Drop()
    {
        pickup.Drop();
    }

    public void _Reset()
    {
        this.transform.localPosition = Vector3.zero;
        this.transform.localRotation = Quaternion.identity;
    }
}
