using System;

namespace DevExpress.GridDemo {
    public class Commodity : ModelObject {
        readonly string name;

        public string Name {
            get { return name; }
        }

        public Commodity(string name) {
            this.name = name;
        }
    }
}

