using UnityEngine;

public sealed class CoinPickup : MonoBehaviour
{
    [SerializeField] private int coinId;

    private GameSession session;
    private Transform player;

    public void Initialize(GameSession owner, Transform target)
    {
        session = owner;
        player = target;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (session == null || other.transform != player)
        {
            return;
        }

        if (session.TryCollect(coinId))
        {
            gameObject.SetActive(false);
        }
    }
}
