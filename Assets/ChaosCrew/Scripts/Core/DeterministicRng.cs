namespace ChaosCrew
{
    /// <summary>
    /// Small seeded xorshift RNG. Keeping match randomness off UnityEngine.Random means a
    /// seed fully reproduces a round, which matters once real networking is added.
    /// </summary>
    public sealed class DeterministicRng
    {
        private uint _state;

        public DeterministicRng(int seed)
        {
            _state = (uint)seed;
            if (_state == 0u) _state = 0x9E3779B9u;
        }

        public uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        /// <summary>Uniform float in [0,1).</summary>
        public float NextFloat() => (NextUInt() & 0x00FFFFFFu) / 16777216f;

        public float Range(float min, float max) => min + NextFloat() * (max - min);

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }

        public bool Chance(float probability) => NextFloat() < probability;

        public void Shuffle<T>(System.Collections.Generic.IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public T Pick<T>(System.Collections.Generic.IList<T> list)
        {
            return list.Count == 0 ? default : list[Range(0, list.Count)];
        }
    }
}
