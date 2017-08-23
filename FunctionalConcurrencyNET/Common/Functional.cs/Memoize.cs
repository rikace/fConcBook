using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional
{
    using static OptionHelpers;

    public static class MemoizeExt
    {
        public static Func<T> memo<T>(Func<T> func)
        {
            var value = new Lazy<T>(func, true);
            return () => value.Value;
        }

        public static Func<T, R> memo<T, R>(Func<T, R> func)
        {
            var cache = new WeakDictionary<T, R>();
            var syncMap = new ConcurrentDictionary<T, object>();

            return inp =>
                cache.TryGetValue(inp).Match(
                    some: x => x,
                    none: () =>
                    {
                        R res;
                        var sync = syncMap.GetOrAdd(inp, new object());
                        lock (sync)
                        {
                            res = cache.GetOrAdd(inp, func);
                        }
                        syncMap.TryRemove(inp, out sync);
                        return res;
                    });
        }

        private class WeakDictionary<T, TR>
        {
            readonly ConcurrentDictionary<T, WeakReference<ActionOnFinalize<TR>>> _dictionary = new ConcurrentDictionary<T, WeakReference<ActionOnFinalize<TR>>>();
            private class ActionOnFinalize<TV>
            {
                public readonly TV Value;
                readonly Action _action;

                public ActionOnFinalize(Action action, TV value)
                {
                    this.Value = value;
                    this._action = action;
                }

                ~ActionOnFinalize()
                {
                    _action();
                }
            }

            private WeakReference<ActionOnFinalize<TR>> ReferenceValue(T key, Func<T, TR> addFunc) =>
                new WeakReference<ActionOnFinalize<TR>>(
                    new ActionOnFinalize<TR>(() =>
                        {
                            WeakReference<ActionOnFinalize<TR>> weakReference;
                            _dictionary.TryRemove(key, out weakReference);
                        },
                        addFunc(key)));

            public Option<TR> TryGetValue(T key)
            {
                WeakReference<ActionOnFinalize<TR>> res = null;
                ActionOnFinalize<TR> target = null;
                return _dictionary.TryGetValue(key, out res)
                    ? res.TryGetTarget(out target)
                        ? Some(target.Value)
                        : None
                    : None;
            }

            public TR GetOrAdd(T key, Func<T, TR> addFunc)
            {
                ActionOnFinalize<TR> target;
                if (_dictionary.GetOrAdd(key, _ => ReferenceValue(key, addFunc)).TryGetTarget(out target))
                    return target.Value;
                throw new OutOfMemoryException();
            }

            public bool TryRemove(T key)
            {
                WeakReference<ActionOnFinalize<TR>> weakReference;
                return _dictionary.TryRemove(key, out weakReference);
            }
        }
    }
}