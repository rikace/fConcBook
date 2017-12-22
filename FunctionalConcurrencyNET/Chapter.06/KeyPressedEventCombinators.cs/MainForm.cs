using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace KeyPressedEventCombinators.cs
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var timerInterval = 5000;
            var secretWord = "reactive";

            // Listing 6.2 Reactive Extension KeyPressedEventCombinators in C#
            var timer = new System.Timers.Timer(timerInterval);
            var timerElapsed = Observable.FromEventPattern<ElapsedEventArgs>
                                (timer, "Elapsed").Select(_ => 'X'); //#A

            var keyPressed = Observable.FromEventPattern<KeyPressEventArgs>
                                (this.textBox, nameof(this.textBox.KeyPress))
                                .Select(kd => Char.ToLower(kd.EventArgs.KeyChar))
                                .Where(c => Char.IsLetter(c));       //#A
            timer.Start();

            timerElapsed
                .Merge(keyPressed)  //#B
                .Scan(String.Empty, (acc, c) =>
                {
                    if (c == 'X') return "Game Over";
                    else
                    {
                        var word = acc + c;
                        if (word == secretWord) return "You Won!";
                        else return word;
                    }   // #C
                })
                .Subscribe(value =>
                    this.label.BeginInvoke(
                        (Action)(() => this.label.Text = value)));
        }
    }
}
