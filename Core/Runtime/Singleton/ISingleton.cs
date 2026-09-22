namespace RiseOn.Utils {
    public interface ISingleton<T> where T : class, ISingleton<T> {
        public static T Ins => SingletonHub.Ins<T>();
        public static bool HasIns => SingletonHub.HasIns<T>();
        public static bool ExistIns => SingletonHub.ExistIns<T>();
    }
}