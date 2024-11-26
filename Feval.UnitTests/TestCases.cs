namespace Feval.UnitTests
{
    public class InstanceMethodCall
    {
        public int IntValue { get; private set; }

        public string StringValue { get; private set; }

        public void SetValue(int value = 100)
        {
            IntValue = value;
        }

        public void SetValue(string stringValue, int intValue = 100)
        {
            StringValue = stringValue;
            IntValue = intValue;
        }
    }

    public static class InstanceMethodCallExtensions
    {
        public static string GetStringValue(this InstanceMethodCall instance)
        {
            instance.SetValue("Hello World");
            return instance.StringValue;
        }

        public static string GetStringValue(this InstanceMethodCall instance, string newValue)
        {
            instance.SetValue(newValue);
            return instance.StringValue;
        }
    }

    public class Vector2
    {
        public int x;

        public int y;

        public Vector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Vector2);
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x + b.x, a.y + b.y);
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x - b.x, a.y - b.y);
        }

        public static Vector2 operator +(Vector2 a, int v)
        {
            return new Vector2(a.x + v, a.y + v);
        }

        public static Vector2 operator -(Vector2 a, int v)
        {
            return new Vector2(a.x - v, a.y - v);
        }

        public static Vector2 operator *(Vector2 a, int v)
        {
            return new Vector2(a.x * v, a.y * v);
        }

        public static Vector2 operator /(Vector2 a, int v)
        {
            return new Vector2(a.x / v, a.y / v);
        }

        protected bool Equals(Vector2 other)
        {
            return x == other.x && y == other.y;
        }

        public override string ToString()
        {
            return $"[{x}, {y}]";
        }
    }

    public class AddTest
    {
        public AddTest(int a, int b)
        {
            m_A = a;
            m_B = b;
        }

        public int Add()
        {
            return Add(m_A, m_B);
        }

        public static int Add(int a, int b)
        {
            return a + b;
        }

        private readonly int m_A;

        private readonly int m_B;
    }
}