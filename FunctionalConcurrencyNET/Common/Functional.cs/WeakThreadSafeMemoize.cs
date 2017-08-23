using System;
using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Threading;

namespace CSMisc
{
/*	namespace MemoizeTest {
		static class Program {

			static readonly Func<int, int> I = I_; 
			static int I_(int a) { return a; }

			static readonly Func<int, int> G = G_; 
			static int G_(int a) { return -a; }

			static void Main(string[] args) { 
				var ma = I.Memoize()(1);
				var mb = G.Memoize()(1);
				Console.WriteLine(ma + " " + mb);
			}
		}
	}
	*/
	[Serializable]
	public class WeakReference<T>: WeakReference where T : class
	{
		public WeakReference(T target): base(target){ }
		public WeakReference(T target, bool trackResurrection): base(target, trackResurrection){ }
		protected WeakReference(SerializationInfo info, StreamingContext context): base(info, context){ }
		public new T Target
		{
			get { return (T)base.Target; }
			set { base.Target = value; }
		}
	}

	public static class Extension
	{
		public static Func<TA, TR> Memoize<TA, TR>(this Func<TA, TR> f)
		{
			return a => MemoizeStorage<Func<TA, TR>, TA, Lazy<TR>>.MapFor(f).GetOrAdd(
				a, arg => new Lazy<TR>(() => f(a), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
		}

		public static Func<TA, Func<TB, TR>> Memoize<TA, TB, TR>(this Func<TA, TB, TR> f)
		{
			return Memoize<TA, Func<TB, TR>>(a => Memoize<TB, TR>(b => f(a, b)));
		}

		public static Func<TA, Func<TB, Func<TC, TR>>>
		Memoize<TA, TB, TC, TR>(this Func<TA, TB, TC, TR> f)
		{
			return Memoize<TA, Func<TB, Func<TC, TR>>>(
				a => Memoize<TB, TC, TR>((b, c) => f(a, b, c)));
		}

		public static Func<TA, Func<TB, Func<TC, Func<TD, TR>>>>
		Memoize<TA, TB, TC, TD, TR>(this Func<TA, TB, TC, TD, TR> f)
		{
			return Memoize<TA, Func<TB, Func<TC, Func<TD, TR>>>>(
				a => Memoize<TB, TC, TD, TR>((b, c, d) => f(a, b, c, d)));
		}

		static class MemoizeStorage<TF, TA, TR>
		{
			static readonly ConcurrentDictionary<TF, ConcurrentDictionary<TA, TR>> Map =
				new ConcurrentDictionary<TF, ConcurrentDictionary<TA, TR>>();

			static public ConcurrentDictionary<TA, TR> MapFor(TF f)
			{ 
				return Map.GetOrAdd(f, arg => new ConcurrentDictionary<TA, TR>());
			}
		}

		public static Func<TA, TR> WeakMemoize<TA, TR>(this Func<TA, TR> f)
		{
			ConcurrentDictionary<TA, WeakReference<Lazy<TR>>> map; 
			WeakMemoizeStorage<Func<TA, TR>, TA, WeakReference<Lazy<TR>>>.MapFor(f, out map); 
			return a => {
				var weakLazy = map.GetOrAdd(a, 
					arg => new WeakReference<Lazy<TR>>(new Lazy<TR>(() => f(a),LazyThreadSafetyMode.ExecutionAndPublication))); 
				var lazy = weakLazy.Target; 
				if (lazy != null) 
					return lazy.Value;

				map.TryRemove(a, out weakLazy); // the GetOrAdd below will always fail if we don't remove a first 

				// force creation since weakref is gone 
				lazy = new Lazy<TR>(() => f(a), LazyThreadSafetyMode.ExecutionAndPublication); 

				weakLazy = map.GetOrAdd(a, arg => new WeakReference<Lazy<TR>>(lazy));
				var actualLazy = weakLazy.Target;

				// actualLazy can be different than lazy (since another thread may have won the GetOrAdd race)
				// prefer actualLazy since it is in the map; but check it for null first since it may have
				// been collected since the map reference is weak.
				return actualLazy != null ? actualLazy.Value : lazy.Value;
			};
		}

		public static Func<TA, Func<TB, TR>> WeakMemoize<TA, TB, TR>(this Func<TA, TB, TR> f)
		{
			return WeakMemoize<TA, Func<TB, TR>>(a => WeakMemoize<TB, TR>(b => f(a, b)));
		}

		public static Func<TA, Func<TB, Func<TC, TR>>> WeakMemoize<TA, TB, TC, TR>(this Func<TA, TB, TC, TR> f)
		{
			return WeakMemoize<TA, Func<TB, Func<TC, TR>>>(a => WeakMemoize<TB, TC, TR>((b, c) => f(a, b, c)));
		}

		

		public static Func<TA, Func<TB, Func<TC, Func<TD, TR>>>> WeakMemoize<TA, TB, TC, TD, TR>(this Func<TA, TB, TC, TD, TR> f)
		{
			return WeakMemoize<TA, Func<TB, Func<TC, Func<TD, TR>>>>(a => WeakMemoize<TB, TC, TD, TR>((b, c, d) => f(a, b, c, d)));
		}

        //static class WeakMemoizeStorage<TF, TA, TR>
        //{
        //	static readonly ConcurrentDictionary<TF, WeakReference<ConcurrentDictionary<TA, TR>>> Map = 
        //	}

        static class WeakMemoizeStorage<TF, TA, TR>
        {
            static readonly ConcurrentDictionary<TF, WeakReference<ConcurrentDictionary<TA, TR>>> Map =
                new ConcurrentDictionary<TF, WeakReference<ConcurrentDictionary<TA, TR>>>();

            static public void MapFor(TF f, out ConcurrentDictionary<TA, TR> map)
            {
                var weakMap = Map.GetOrAdd(f, arg => new WeakReference<ConcurrentDictionary<TA, TR>>(new ConcurrentDictionary<TA, TR>()));

                map = weakMap.Target;
                if (map != null)
                    return;

                Map.TryRemove(f, out weakMap); // have to remove f for the GetOrAdd below to work
                var newMap = new ConcurrentDictionary<TA, TR>();
                weakMap = Map.GetOrAdd(f, arg => new WeakReference<ConcurrentDictionary<TA, TR>>(newMap));
                var actualMap = weakMap.Target;

                // prefer actualMap, since it's in the Map
                map = actualMap ?? newMap;
            }
        }
    }
}

