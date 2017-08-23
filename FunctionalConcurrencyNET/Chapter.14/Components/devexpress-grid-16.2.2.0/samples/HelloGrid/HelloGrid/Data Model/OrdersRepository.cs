using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HelloGrid
{
	public abstract class OrdersRepository {
		readonly ObservableCollection<Order> orders;

		public OrdersRepository() {
			this.orders = new ObservableCollection<Order>();
		}

		public ObservableCollection<Order> Orders {
			get { return orders; }
		}
	}

	public class TestOrdersRepository : OrdersRepository {

		const int orderCount = 100;
		readonly List<Product> products;
		readonly Random random;

		public TestOrdersRepository() : base() {
			this.random = new Random((int)DateTime.Now.Ticks);
			this.products = new List<Product>();

			GenerateProducts();

			for (int i = 0; i < orderCount; i++)
				Orders.Add(GenerateOrder(i));
		}

		Order GenerateOrder(int number) {
			Order order = new Order(new DateTime(2014, 1, 1).AddDays(random.Next(0, 60)), 
				number % 3 == 0, RandomItem<Product>(products), random.Next(1, 100));
			return order;
		}

		T RandomItem<T>(IList<T> list) {
			int index = (int)(random.NextDouble() * 0.99 * (list.Count));
			return list[index];
		}

		void GenerateProducts() {
			products.Add(new Product("Tofu", 50));
			products.Add(new Product("Chocolade", 34));
			products.Add(new Product("Ikura", 70));
			products.Add(new Product("Chai", 3));
			products.Add(new Product("Boston Crab Meat", 36));
			products.Add(new Product("Ravioli Angelo", 18));
			products.Add(new Product("Ipon Coffee", 10));
			products.Add(new Product("Questo Cabrales", 25));
		}
	}
}

