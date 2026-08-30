/// <summary>
/// Small deterministic random generator whose state can be persisted with a run.
/// </summary>
public sealed class RunRandom
{
    private int state;

    public int State => state;

    public RunRandom(int seed)
    {
        state = seed == 0 ? 0x13579BDF : seed;
    }

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0) return 0;
        return NextPositive() % maxExclusive;
    }

    public double NextDouble()
    {
        return NextPositive() / 2147483648d;
    }

    private int NextPositive()
    {
        unchecked { state = state * 1103515245 + 12345; }
        return state & int.MaxValue;
    }
}
