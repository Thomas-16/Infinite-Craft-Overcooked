using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupableObject : MonoBehaviour
{
    public Player HoveringPlayer = null;
    public Player PickedupByPlayer = null;

    [field: SerializeField]
    public bool IsPickedUp { get; protected set; }

    [SerializeField] private GameObject hoverVisual;
    [SerializeField] protected Collider[] mainColliders;

    private float pickupThresholdTimer;

    protected Transform oldParent;

    protected virtual void Update()
    {
        // Only update hover visual, don't reset HoveringPlayer
        if (hoverVisual != null)
        {
            hoverVisual.SetActive(HoveringPlayer != null && !IsPickedUp);
        }

        pickupThresholdTimer -= Time.deltaTime;
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
    public void SetPickupTimer(float time) {
        pickupThresholdTimer = time;
    }
    public bool CanBePickedup() {
        return pickupThresholdTimer <= 0f;
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