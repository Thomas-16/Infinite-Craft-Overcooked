using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupableObject : MonoBehaviour
{
    // TODO: refactor to be a player id for multiplayer
    public Player HoveringPlayer { get; private set; } = null;
    public Player PickedupByPlayer { get; private set; } = null;

    [field: SerializeField]
    public bool IsPickedUp { get; private set; }

    [SerializeField] private GameObject hoverVisual;
    [SerializeField] private Collider[] mainColliders;

    private Transform oldParent;

    protected void Update() {
        hoverVisual.SetActive(HoveringPlayer != null && !IsPickedUp);

        // Reset IsHovered to false at the beginning of the frame
        HoveringPlayer = null;
    }
    public void Pickup(Player player) {
        PickedupByPlayer = player;
        IsPickedUp = true;

        foreach(Collider collider in mainColliders) {
            collider.enabled = false;
        }
        GetComponent<Rigidbody>().isKinematic = true;
        oldParent = transform.parent;
        transform.parent = PickedupByPlayer.GetHoldingObjectSpotTransform();
        transform.position = PickedupByPlayer.GetHoldingObjectSpotTransform().position;
    }
    public void Drop(Player player) {
        if(player != PickedupByPlayer) {
            return;
        }

        PickedupByPlayer = null;
        IsPickedUp = false;

        foreach (Collider collider in mainColliders) {
            collider.enabled = true;
        }
        GetComponent<Rigidbody>().isKinematic = false;
        transform.SetParent(oldParent, true);
    }
    public void HoverOver(Player player) {
        // Set IsHovered to true when HoverOver is called
        HoveringPlayer = player;
    }
}
