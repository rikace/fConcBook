using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public class Customer : ModelObject, IComparable<Customer>, IEquatable<Customer> {
        string name;

        public string Name {
            get { return name; }
			set {
                name = value;
                if (Photo == null) {
                    string resourceName = value.Replace(" ", String.Empty);
                    if (!String.IsNullOrEmpty(resourceName))
                        Photo = ImageSource.FromResource(resourceName);
                }
            }
        }

        public Customer(string name) {
            this.Name = name;
        }

        public ImageSource Photo { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime HireDate { get; set; }
        public string Position { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Notes { get; set; }
		public string Email { get; set; }

        public int CompareTo(Customer other) {
            return Comparer<string>.Default.Compare(this.Name, other.Name);
        }

        bool IEquatable<Customer>.Equals(Customer other) {
            return this.Name == other.Name;
        }
    }
}

