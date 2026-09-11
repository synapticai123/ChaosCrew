using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    /// <summary>Result of one player's ballot. -1 means "skip".</summary>
    public struct Ballot
    {
        public int VoterId;
        public int TargetId;
    }

    /// <summary>
    /// The accusation phase. Bots vote from what the footage appeared to show, so a
    /// successful false-trail sabotage genuinely misleads them.
    /// </summary>
    public sealed class VoteSystem
    {
        public const int SkipVote = -1;

        public readonly List<Ballot> Ballots = new List<Ballot>();

        private MatchState _state;
        private Dictionary<int, float> _suspicion = new Dictionary<int, float>();

        public bool LocalVoteCast { get; private set; }
        public int LocalVote { get; private set; } = SkipVote;

        public void Begin(MatchState state, Dictionary<int, float> suspicion)
        {
            _state = state;
            _suspicion = suspicion ?? new Dictionary<int, float>();
            Ballots.Clear();
            LocalVoteCast = false;
            LocalVote = SkipVote;
        }

        public float SuspicionOf(int playerId) =>
            _suspicion.TryGetValue(playerId, out float v) ? v : 0f;

        public void CastLocalVote(int targetId)
        {
            if (LocalVoteCast) return;
            LocalVoteCast = true;
            LocalVote = targetId;
            Ballots.Add(new Ballot { VoterId = _state.LocalPlayerId, TargetId = targetId });
        }

        /// <summary>Bots decide once the human has voted or the clock runs out.</summary>
        public void CastBotVotes()
        {
            for (int i = 0; i < _state.Players.Count; i++)
            {
                PlayerState voter = _state.Players[i];
                if (!voter.IsBot) continue;
                if (HasVoted(voter.Id)) continue;
                Ballots.Add(new Ballot { VoterId = voter.Id, TargetId = DecideBotVote(voter) });
            }
        }

        private bool HasVoted(int id)
        {
            for (int i = 0; i < Ballots.Count; i++)
                if (Ballots[i].VoterId == id) return true;
            return false;
        }

        private int DecideBotVote(PlayerState voter)
        {
            int best = SkipVote;
            float bestScore = 0.9f; // below this nobody looks guilty enough to bother

            for (int i = 0; i < _state.Players.Count; i++)
            {
                PlayerState target = _state.Players[i];
                if (target.Id == voter.Id) continue;

                float score = SuspicionOf(target.Id);

                // A saboteur bot never votes for itself and nudges suspicion elsewhere.
                if (voter.IsSaboteur) score += target.IsSaboteur ? -99f : _state.Rng.Range(0.5f, 2.5f);
                else score += _state.Rng.Range(0f, 1.1f);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = target.Id;
                }
            }
            return best;
        }

        public int CountFor(int targetId)
        {
            int n = 0;
            for (int i = 0; i < Ballots.Count; i++)
                if (Ballots[i].TargetId == targetId) n++;
            return n;
        }

        /// <summary>Highest vote count wins; a tie ejects nobody.</summary>
        public int Tally(out bool tied)
        {
            var counts = new Dictionary<int, int>();
            for (int i = 0; i < Ballots.Count; i++)
            {
                counts.TryGetValue(Ballots[i].TargetId, out int c);
                counts[Ballots[i].TargetId] = c + 1;
            }

            int bestId = SkipVote;
            int bestCount = 0;
            tied = false;

            foreach (var kv in counts)
            {
                if (kv.Value > bestCount)
                {
                    bestCount = kv.Value;
                    bestId = kv.Key;
                    tied = false;
                }
                else if (kv.Value == bestCount)
                {
                    tied = true;
                }
            }

            if (tied || bestId == SkipVote)
            {
                tied = true;
                return SkipVote;
            }
            return bestId;
        }
    }
}
