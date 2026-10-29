using System;
using System.Collections.Generic;

public sealed class CoinRound
{
    private readonly HashSet<int> collectedIds = new HashSet<int>();

    public int Score { get; private set; }
    public float Remaining { get; private set; }

    public bool IsFinished => Remaining <= 0f;

    public CoinRound(float seconds)
    {
        Reset(seconds);
    }

    public bool TryCollect(int coinId)
    {
        if (IsFinished || !collectedIds.Add(coinId))
        {
            return false;
        }

        Score += 1;
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        Remaining = Math.Max(0f, Remaining - deltaTime);
    }

    public void Reset(float seconds)
    {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        collectedIds.Clear();
        Score = 0;
        Remaining = seconds;
    }
}
