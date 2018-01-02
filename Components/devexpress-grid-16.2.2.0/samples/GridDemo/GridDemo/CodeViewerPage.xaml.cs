using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class CodeViewerPage {
		public static readonly BindableProperty TextProperty = BindableProperty.Create("Text", typeof(string), typeof(CodeViewerPage), default(string));

        public CodeViewerPage() {
            InitializeComponent();
            this.BindingContext = this;
        }

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
    }
}
