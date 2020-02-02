using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Timers;

namespace KeyPressedEventCombinators.CSharp
{
    internal class Program
    {
        private static IObservable<string> ConsoleInput()
        {
            return
                Observable
                    .FromAsync(() => Console.In.ReadLineAsync())
                    .Repeat()
                    .Publish()
                    .RefCount()
                    .SubscribeOn(Scheduler.Default);
        }

        private static IObservable<char> InterceptKeys()
        {
            IEnumerable<char> InnerInterceptor()
            {
                char keyPressed;
                while ((keyPressed = Console.ReadKey().KeyChar) != Convert.ToChar(ConsoleKey.Enter))
                    yield return keyPressed;
            }

            return InnerInterceptor().ToObservable();
        }

        private static void Main(string[] args)
        {
            var timerInterval = 5000;
            var secretWord = "reactive";

            // Listing 6.2 Reactive Extension KeyPressedEventCombinators in C#
            var timer = new Timer(timerInterval);
            var timerElapsed = Observable.FromEventPattern<ElapsedEventArgs>
                (timer, "Elapsed").Select(_ => 'X'); //#A

            // var keyPressed = Observable.FromEventPattern<KeyPressEventArgs>
            //         (this.textBox, nameof(this.textBox.KeyPress))
            //     
            //     .Select(kd => Char.ToLower(kd.EventArgs.KeyChar))
            //     .Where(c => Char.IsLetter(c));       //#A

            var keyPressed = InterceptKeys();
            timer.Start();

            timerElapsed
                .Merge(keyPressed) //#B
                .Scan(string.Empty, (acc, c) =>
                {
                    if (c == 'X') return "Game Over";

                    var word = acc + c;
                    if (word == secretWord) return "You Won!";
                    return word;
                })
                .Subscribe(value =>
                    Console.WriteLine($"{DateTime.Now:MM/dd/yy H:mm:ss} - {value}"));

            Console.ReadLine();
        }
    }
}