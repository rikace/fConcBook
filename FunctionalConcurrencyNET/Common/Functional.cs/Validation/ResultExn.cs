using Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional.Validation
{
    public struct ResultExn<T>
    {
        internal Exception Ex { get; }
        internal T Value { get; }

        public bool Success => Ex == null;
        public bool Exception => Ex != null;

        internal ResultExn(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            Ex = ex;
            Value = default(T);
        }

        internal ResultExn(T right)
        {
            Value = right;
            Ex = null;
        }

        public static implicit operator ResultExn<T>(Exception error) => new ResultExn<T>(error);
        public static implicit operator ResultExn<T>(T value) => new ResultExn<T>(value);

        public TR Match<TR>(Func<Exception, TR> Exception, Func<T, TR> Success)
           => this.Exception ? Exception(Ex) : Success(Value);

        public Unit Match(Action<Exception> Exception, Action<T> Success)
           => Match(Exception.ToFunc(), Success.ToFunc());
    }
}