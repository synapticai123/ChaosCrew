using System.Collections.Generic;

namespace ChaosCrew
{
    public struct SeatInfo
    {
        public int Id;
        public string DisplayName;
        public int ColorIndex;
        public bool IsLocal;
        public bool IsBot;
        public bool Ready;
    }

    /// <summary>
    /// Everything the game needs from a session provider. The prototype ships
    /// <see cref="LocalTransport"/> (human + bots in one process); an online implementation
    /// slots in here without the game layer changing.
    /// </summary>
    public interface IMatchTransport
    {
        bool IsHost { get; }
        int LocalSeatId { get; }
        IReadOnlyList<SeatInfo> Seats { get; }

        /// <summary>Raised whenever the seat list changes (join, leave, ready toggle).</summary>
        event System.Action SeatsChanged;
        /// <summary>Raised when the host starts the match; carries the shared random seed.</summary>
        event System.Action<int> MatchStarted;

        void OpenLobby(string localName);
        void SetReady(int seatId, bool ready);
        void RequestStart();
        void Tick(float dt);
        void Leave();
    }

    /// <summary>
    /// Local session: seat 0 is the human, the rest fill in as bots over a couple of seconds
    /// so the lobby has the rhythm of players actually arriving.
    /// </summary>
    public sealed class LocalTransport : IMatchTransport
    {
        private static readonly string[] BotNames =
        {
            "Rusty", "Pixel", "Momo", "Bopp", "Cleo", "Nils", "Zaza", "Quirin"
        };

        private readonly List<SeatInfo> _seats = new List<SeatInfo>();
        private float _fillTimer;
        private bool _started;
        private DeterministicRng _rng;

        public bool IsHost => true;
        public int LocalSeatId => 0;
        public IReadOnlyList<SeatInfo> Seats => _seats;

        public event System.Action SeatsChanged;
        public event System.Action<int> MatchStarted;

        public void OpenLobby(string localName)
        {
            _rng = new DeterministicRng(System.Environment.TickCount);
            _seats.Clear();
            _started = false;
            _fillTimer = 0.5f;

            _seats.Add(new SeatInfo
            {
                Id = 0,
                DisplayName = string.IsNullOrEmpty(localName) ? "Du" : localName,
                ColorIndex = 0,
                IsLocal = true,
                IsBot = false,
                Ready = false
            });
            SeatsChanged?.Invoke();
        }

        public void SetReady(int seatId, bool ready)
        {
            for (int i = 0; i < _seats.Count; i++)
            {
                if (_seats[i].Id != seatId) continue;
                SeatInfo s = _seats[i];
                s.Ready = ready;
                _seats[i] = s;
                SeatsChanged?.Invoke();
                return;
            }
        }

        public void RequestStart()
        {
            if (_started) return;
            _started = true;
            MatchStarted?.Invoke(_rng != null ? (int)_rng.NextUInt() : System.Environment.TickCount);
        }

        public void Tick(float dt)
        {
            if (_started || _seats.Count >= CCConfig.PlayerCount) return;

            _fillTimer -= dt;
            if (_fillTimer > 0f) return;
            _fillTimer = _rng.Range(0.45f, 1.15f);

            int id = _seats.Count;
            _seats.Add(new SeatInfo
            {
                Id = id,
                DisplayName = BotNames[_rng.Range(0, BotNames.Length)],
                ColorIndex = id,
                IsLocal = false,
                IsBot = true,
                Ready = true
            });
            SeatsChanged?.Invoke();
        }

        public void Leave()
        {
            _seats.Clear();
            _started = false;
            SeatsChanged?.Invoke();
        }

        public bool AllReady()
        {
            if (_seats.Count < CCConfig.PlayerCount) return false;
            for (int i = 0; i < _seats.Count; i++)
                if (!_seats[i].Ready) return false;
            return true;
        }
    }
}
