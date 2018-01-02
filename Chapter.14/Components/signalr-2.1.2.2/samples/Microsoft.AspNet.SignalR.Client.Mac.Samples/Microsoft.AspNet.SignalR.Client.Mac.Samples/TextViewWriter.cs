using System;
using System.IO;
using System.Threading;
using Foundation;
using CoreFoundation;
using AppKit;

namespace SignalRMac
{
    public class TextViewWriter : TextWriter
    {
        private SynchronizationContext _context;
        private NSTextView _textView;

        public TextViewWriter(SynchronizationContext context, NSTextView textView)
        {
            _context = context;
            _textView = textView;
        }

        public override void WriteLine(string value)
        {
            _context.Post(delegate
                {
                    _textView.Value = _textView.Value + value + Environment.NewLine;
                }, state: null);
        }

        public override void WriteLine(string format, object arg0)
        {
            this.WriteLine(string.Format(format, arg0));
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            this.WriteLine(string.Format(format, arg0, arg1, arg2));
        }

        public override void WriteLine(string format, params object[] args)
        {
            _context.Post(delegate
                {
                    _textView.Value = _textView.Value + string.Format(format, args) + Environment.NewLine;
                    _textView.ScrollRangeToVisible(new NSRange(_textView.Value.Length, 0));

                }, state: null);
        }

        #region implemented abstract members of TextWriter

        public override System.Text.Encoding Encoding
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}

