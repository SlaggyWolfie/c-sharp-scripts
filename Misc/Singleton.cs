namespace Slaggy
{
    public class Singleton<T> where T : class, new()
    {
        private static T _instance = null;
        public static T instance => _instance ??= new T();

        protected Singleton() { }
    }
}
