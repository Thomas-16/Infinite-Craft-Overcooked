using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupableObject : MonoBehaviour
{
    public Player HoveringPlayer { get; protected set; } = null;
    public Player PickedupByPlayer { get; private set; } = null;

    [field: SerializeField]
    public bool IsPickedUp { get; protected set; }

    [SerializeField] private GameObject hoverVisual;
    [SerializeField] protected Collider[] mainColliders;

    protected Transform oldParent;

    protected virtual void Update()
    {
        // Only update hover visual, don't reset HoveringPlayer
        if (hoverVisual != null)
        {
            hoverVisual.SetActive(HoveringPlayer != null && !IsPickedUp);
        }
    }

    public virtual void Pickup(Player player)
    {
        PickedupByPlayer = player;
        IsPickedUp = true;
        HoveringPlayer = null; // Clear hover state when picked up

        foreach (Collider collider in mainColliders)
        {
            collider.enabled = false;
        }
        GetComponent<Rigidbody>().isKinematic = true;
        oldParent = transform.parent;
        transform.parent = PickedupByPlayer.GetHoldingObjectSpotTransform();
        transform.position = PickedupByPlayer.GetHoldingObjectSpotTransform().position;
    }

    public virtual void Drop(Player player)
    {
        if (player != PickedupByPlayer)
        {
            return;
        }

        PickedupByPlayer = null;
        IsPickedUp = false;

        foreach (Collider collider in mainColliders)
        {
            collider.enabled = true;
        }
        GetComponent<Rigidbody>().isKinematic = false;
        transform.SetParent(oldParent, true);
    }

    public virtual void HoverOver(Player player)
    {
        HoveringPlayer = player;
    }

    public virtual void ClearHover()
    {
        HoveringPlayer = null;
    }
}