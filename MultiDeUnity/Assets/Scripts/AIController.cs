using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class AIController : NetworkBehaviour
{
    public enum AIType
    {
        Simple = 0,
        Smart = 1
    }

    [Header("AI Type")]
    public AIType aiType = AIType.Smart;

    [Header("Movement")]
    [Tooltip("World units per second")]
    public float moveSpeed = 5f;

    [Tooltip("How close to the coin before attempting pickup")]
    public float pickupRange = 1.2f;

    [Tooltip("Small distance to stop moving when very close to target")]
    public float stopDistance = 0.15f;

    [Header("Decision")]
    [Tooltip("How often (seconds) to pick/reevaluate target. Movement is continuous every network tick.")]
    public float decisionInterval = 0.25f;

    [Tooltip("If another player is this fraction closer to a coin than the AI, the Smart AI will skip that coin.")]
    [Range(0f, 1f)]
    public float avoidThreshold = 0.75f;

    private float decisionTimer = 0f;
    private PlayerController pc;
    private Coin targetCoin = null;

    public override void Spawned()
    {
        base.Spawned();
        pc = GetComponent<PlayerController>();
        decisionTimer = 0f;
        targetCoin = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (pc == null) return;
        if (!pc.IsAI) return;
        if (!pc.CanMove) return;

        decisionTimer -= Runner.DeltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = Mathf.Max(0.01f, decisionInterval);
            ChooseTargetCoin();
        }

        if (targetCoin != null)
        {
            if (targetCoin == null)
            {
                targetCoin = null;
                return;
            }

            Vector3 myPos = transform.position;
            Vector3 targetPos = targetCoin.transform.position;
            Vector3 dir = targetPos - myPos;
            dir.y = 0f;

            float dist = dir.magnitude;

            if (dist <= pickupRange)
            {
                targetCoin.ProcessPickupBy(pc);

                targetCoin = null;
                return;
            }

            if (dist <= stopDistance)
            {
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                    pc.SetNetworkedTransform(transform.position, targetRot);
                }
                return;
            }

            Vector3 move = dir.normalized * moveSpeed * Runner.DeltaTime;

            if (move.magnitude > dist)
                move = dir;

            Vector3 newPos = transform.position + move;
            Quaternion newRot = (dir.sqrMagnitude > 0.001f) ? Quaternion.LookRotation(dir.normalized) : transform.rotation;

            pc.SetNetworkedTransform(newPos, newRot);

            //pc.MoveNetworked(move);
        }
    }

    private void ChooseTargetCoin()
    {
        targetCoin = null;

        var coins = Coin.ActiveCoins;
        if (coins == null || coins.Count == 0) return;

        Vector3 myPos = transform.position;
        float bestDist = float.MaxValue;
        Coin best = null;

        foreach (var c in coins)
        {
            if (c == null) continue;

            float d = (c.transform.position - myPos).sqrMagnitude;

            if (aiType == AIType.Smart)
            {
                bool someoneMuchCloser = false;
                foreach (var playerRef in Runner.ActivePlayers)
                {
                    var pObj = Runner.GetPlayerObject(playerRef);
                    if (pObj == null) continue;
                    var otherPc = pObj.GetComponent<PlayerController>();
                    if (otherPc == null) continue;

                    if (otherPc == pc) continue;
                    float otherD = (otherPc.transform.position - c.transform.position).sqrMagnitude;
                    if (otherD < d * avoidThreshold)
                    {
                        someoneMuchCloser = true;
                        break;
                    }
                }
                if (someoneMuchCloser) continue;
            }

            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }

        targetCoin = best;
    }

    public void SetAsAI()
    {
        if (!Object.HasStateAuthority) return;
        if (pc == null) pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.IsAI = true;
            pc.CanMove = true;
        }
    }
}
