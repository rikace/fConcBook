using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;

namespace Common
{
    public class FileLinesStream<T>
    {
        private readonly List<T> _data;

        private readonly string _filePath;
        private readonly Func<string, T> _map;

        public FileLinesStream(string filePath, Func<string, T> map)
        {
            _filePath = filePath;
            _map = map;
            _data = new List<T>();
        }

        public IEnumerable<T> GetLines()
        {
            using (var stream = File.OpenRead(Path.Combine("../../../../../../Common/Data/Tickers", _filePath)))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var value = _map(line);
                    if (value != null)
                        _data.Add(value);
                }
            }

            _data.Reverse();
            while (true)
                foreach (var item in _data)
                    yield return item;
        }

        public IObservable<T> ObserveLines()
        {
            return GetLines().ToObservable();
        }

        public IObservable<T> ObserveLines2()
        {
            using (var stream = File.OpenRead(Path.Combine("Tickers", _filePath)))
            using (var reader = new StreamReader(stream))
            {
                return reader.ToLineaObservable().Select(_map);
            }
        }
    }

    public static class TextReaderExtensions
    {
        public static IObservable<string> ToLineaObservable2(this StreamReader reader)
        {
            return Observable.Create<string>(async (observer, token) =>
            {
                try
                {
                    while (!token.IsCancellationRequested && !reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(line))
                            break;

                        observer.OnNext(line);
                    }

                    observer.OnCompleted();
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
            });
        }

        public static IObservable<string> ToLineaObservable(this StreamReader reader)
        {
            return Observable.Create<string>(async (observer, token) =>
            {
                try
                {
                    while (!token.IsCancellationRequested && !reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(line))
                            break;

                        observer.OnNext(line);
                    }

                    observer.OnCompleted();
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
            });
        }
    }
}