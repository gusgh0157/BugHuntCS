namespace BugHunt;

// 길찾기에서 이미 가 본 칸을 HashSet 으로 기억한다. 결과는 맞는데 맵이 커질수록 급격히 느려진다.
public static class ChallengeB3
{
    readonly struct Cell(int x, int y) : IEquatable<Cell>
    {
        public static long Comparisons;
        public int X { get; } = x;
        public int Y { get; } = y;
        public bool Equals(Cell other) { Comparisons++; return X == other.X && Y == other.Y; }
        public override bool Equals(object? obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => X ^ Y;
    }

    // 벽이 없는 맵에서 (0,0)부터 넓이 우선 탐색. 도달한 칸 수를 돌려준다
    static int Flood(int size, Func<int, int, bool> wall)
    {
        var visited = new HashSet<Cell> { new(0, 0) };
        var queue = new Queue<Cell>();
        queue.Enqueue(new Cell(0, 0));
        while (queue.Count > 0)
        {
            Cell c = queue.Dequeue();
            foreach ((int dx, int dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = c.X + dx, ny = c.Y + dy;
                if (nx < 0 || ny < 0 || nx >= size || ny >= size || wall(nx, ny))
                    continue;
                if (visited.Add(new Cell(nx, ny)))
                    queue.Enqueue(new Cell(nx, ny));
            }
        }
        return visited.Count;
    }

    public static void Run()
    {
        int size = Pick(128, 96, 192);
        int gap = Pick(7, 5, 11);
        bool Wall(int x, int y) => x % gap == gap - 1 && y % gap != 0;

        Cell.Comparisons = 0;
        int reached = 0;
        (_, long ms) = Measure(() => reached = Flood(size, Wall));
        Console.WriteLine($"  {size}x{size} 맵, 도달한 칸 {reached:N0}개, 비교 {Cell.Comparisons:N0}번, {ms}ms");

        int open = 0;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (!Wall(x, y))
                    open++;
        Expect("도달한 칸 수", open, reached);
        ExpectAtMost("칸 하나당 비교 횟수", 4, Cell.Comparisons / reached);
    }
}
