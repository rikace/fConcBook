This tutorial provides an overview of the capabilities you’ll find in the DevExpress Grid Control – our feature-rich and FREE Grid Control for Xamarin.Forms. The step-by-step instructions that follow are designed to help you create a cross-platform application for Android, iOS and Windows Phone and to integrate our Outlook-inspired Grid Control within it.

The tutorial is divided into the following sections:  

* Create a Solution
* Add the DevExpress Grid Control
* Generate the Data Source
* Bind the Grid to Data and Create Columns
* New Item Row
* Data Sorting
* Data Grouping
* Data Summaries
* Data Filtering 

*Note: Once you bind the grid to data, you can execute the app after each individual step to view intermediate results.  You can also skip tutorial steps if you’d like to explore a specific feature shipping inside the DevExpress Grid.*  

#Create a Solution  
Create a new Xamarin.Forms Portable solution (*HelloGrid*) that includes Android, iOS, Windows Phone and PCL projects.    

Add the DevExpress Grid component to your Android and iOS projects using the Component Manager. References to the **DevExpress.Mobile.Grid.Android.v16.2.dll**, **DevExpress.Mobile.Core.Android.v16.2.dll**, **DevExpress.Mobile.Grid.iOS.v16.2.dll** and **DevExpress.Mobile.Core.iOS.v16.2.dll** libraries will be automatically added to your corresponding projects.  

Next, manually add the **DevExpress.Mobile.Grid.WinPhone.v16.2.dll** and **DevExpress.Mobile.Core.WinPhone.v16.2.dll** assembly references to the Windows Phone project, and the **DevExpress.Mobile.Grid.v16.2.dll** and **DevExpress.Mobile.Core.v16.2.dll** assembly references to the PCL project. These libraries have been automatically copied to the following path within your solution folder.  

*Components/devexpress—grid-16.2.2.0/lib/*  

Add the following initialization code to your Android (the *MainActivity.cs* file), iOS (the *AppDelegate.cs* file) and Windows Phone (the *MainPage.xaml.cs* file) projects.

C#  
```csharp  
DevExpress.Mobile.Forms.Init ();  
```
#Add the DevExpress Grid Control  
Add a new content page (*MainPage*) to your PCL project. To set the start page for the application, modify the **App** class as follows:  

C#  
```csharp   
public class App : Application {
    public App() {
        this.MainPage = GetMainPage();
    }
    public static Page GetMainPage () {
        return new MainPage ();
    }
}
```
Add the DevExpress Grid Control to your page.  

XAML (the *MainPage.xaml* file)  
```xml
<ContentPage xmlns="http://xamarin.com/schemas/2014/forms" 
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml" 
             x:Class="HelloGrid.MainPage" 
             xmlns:dxGrid="clr-namespace:DevExpress.Mobile.DataGrid;assembly=DevExpress.Mobile.Grid.v16.2"> 
    <dxGrid:GridControl x:Name="grid"> 
    </dxGrid:GridControl> 
</ContentPage>
```

#Generate the Data Source  
In this step, you will create an in-memory data source object and populate it with data.  

*Note: Though the DevExpress Grid fully supports standard Xamarin data binding mechanisms, this tutorial uses an in-memory dataset to avoid dependence on external files or databases.*  

Declare the **Order** class encapsulating an individual data record. Its public properties (**Date**, **Shipped**, **Product** and **Quantity**) will serve as data source fields.  

C#  
```csharp 
public class Order : ModelObject {

    DateTime date;
    bool shipped;
    Product product;
    int quantity;

    public Order() {
        this.date = DateTime.Today;
        this.shipped = false;
        this.product = new Product ("", 0);
        this.quantity = 0;
    }

    public Order(DateTime date, bool shipped, Product product, int quantity) {
        this.date = date;
        this.shipped = shipped;
        this.product = product;
        this.quantity = quantity;
    }

    public DateTime Date {
        get { return date; }
        set { if (date != value) {
                date = value;
                RaisePropertyChanged("Date");}}
    }

    public bool Shipped {
        get { return shipped; }
        set { if(shipped != value) {
                shipped = value;
                RaisePropertyChanged("Shipped");}}
    }

    public Product Product {
        get { return product; }
        set { if (product != value) {
                product = value; 
                RaisePropertyChanged ("Product");}}
    }

    public int Quantity {
        get { return quantity; }
        set { if (quantity != value) {
                quantity = value; 
                RaisePropertyChanged ("Quantity");}}
    }
}

public class Product : ModelObject {
    string name;
    int unitPrice;

    public Product(string name, int unitPrice) {
        this.name = name;
        this.unitPrice = unitPrice;
    }

    public string Name {
        get { return name; }
        set { name = value; }
    }

    public int UnitPrice{
        get { return unitPrice; }
        set { unitPrice = value; }
    }
}

public class ModelObject : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    protected void RaisePropertyChanged(string name) {
        if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(name));
    }
}
```

A collection of **Order** objects will represent the Grid’s data source. This collection is returned by the **Orders** property of the **TestOrdersRepository** class.  

C#  
```csharp 
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
```

#Bind the DevExpress Grid to Data and Create Columns  
Set **BindingContext** for the content page to an instance of the **TestOrdersRepository** class (in the *MainPage.xaml.cs* file) as demonstrated below.  

C#  
```csharp
namespace HelloGrid {    
    public partial class MainPage : ContentPage {    
        public MainPage () {
            InitializeComponent ();

            TestOrdersRepository model = new TestOrdersRepository ();
            BindingContext = model;
        }
    }
}
```
To bind the Grid to a data source, assign the order collection object (**TestOrdersRepository.Orders**) to the **GridControl.ItemsSource** property.  

Once the grid is bound to the data source, create columns and bind them to data fields. The following column types (**GridColumn** descendant classes) are available for use in the Grid Control: **TextColumn**, **NumberColumn**, **DateColumn**, **SwitchColumn** or **ImageColumn**.  

Create appropriate column objects, bind each column to the corresponding data field using the **GridColumn.FieldName** property and add columns to the **GridControl.Columns** collection.  

You can create unbound columns and display calculated values based upon formulas applied against other columns. To start, add the appropriate column object to the **GridControl.Columns** collection and set the following column properties.  

* **GridColumn.FieldName** - a unique string, one that does not match any field name in the Grid Control's underlying data source.  
* **GridColumn.UnboundExpression** - a formula (string expression) to automatically evaluate values for the column.  
* **GridColumn.UnboundType** - column data type (Boolean, DateTime, Decimal, Integer, String or Object).  

In the following example, the Total column is unbound and displays **Quantity** multiplied by **UnitPrice**.  


XAML  
```xml 
<dxGrid:GridControl x:Name="grid" ItemsSource="{Binding Orders}"> 
    <dxGrid:GridControl.Columns> 
         <dxGrid:TextColumn FieldName="Product.Name" Caption = "Product" Width = "170" /> 
         <dxGrid:NumberColumn FieldName="Product.UnitPrice" Caption = "Price" DisplayFormat="C0"/> 
         <dxGrid:NumberColumn FieldName="Quantity"/> 
         <dxGrid:NumberColumn FieldName="Total" 
                              UnboundType="Integer" UnboundExpression="[Quantity] * [Product.UnitPrice]" 
                              IsReadOnly="True" DisplayFormat="C0"/> 
         <dxGrid:DateColumn FieldName="Date" DisplayFormat="d"/> 
         <dxGrid:SwitchColumn FieldName="Shipped" /> 
    </dxGrid:GridControl.Columns> 
</dxGrid:GridControl> 
```

#New Item Row  
To help simplify data entry by your end-users, the DevExpress Grid includes a Microsoft Outlook-inspired New Item Row option. To activate it, set the **GridControl.NewItemRowVisibility** property to **true** as illustrated below.

XAML  
```xml
<dxGrid:GridControl x:Name="grid" ItemsSource="{Binding Orders}" NewItemRowVisibility = "true"> 
    <!-- ... --> 
</dxGrid:GridControl>
```

#Data Sorting  
*By default*, the DevExpress Grid will sort data against a single column. To initiate sorting, set the desired column’s **GridColumn.SortOrder** property to **Ascending** or **Descending**. Once sort order is selected, the Grid will first clear all previously applied sort operations and then re-sort data as specified.  

*To sort data against multiple columns*, set the **GridControl.SortMode** property to **GridSortMode.Multiple**, and specify **GridColumn.SortOrder** for the desired columns. To specify sort order priority, use the **GridColumn.SortIndex** property for your sorted columns.  

To disable end-user sorting, use the **GridColumn.AllowSort** property.  

The following example sorts orders by **Product.Name** and **Quantity** and disables end-user sorting for the **Shipped** column.  

XAML    
```xml
<dxGrid:GridControl x:Name="grid" ItemsSource="{Binding Orders}" 
	 				NewItemRowVisibility = "true" 
					CalculateCustomSummary="grid_CustomSummary" 
					SortMode = "Multiple">
	<dxGrid:GridControl.Columns>
	 	<dxGrid:TextColumn FieldName="Product.Name" Caption = "Product" Width = "170" 
							SortOrder = "Descending" SortIndex = "0"/>
	 	<!-- ... -->
	 	<dxGrid:NumberColumn FieldName="Quantity" 
	 						 SortOrder = "Ascending" SortIndex = "1"/>
	 	<!-- ... -->
	 	<dxGrid:SwitchColumn FieldName="Shipped" AllowSort = "False"/>
	</dxGrid:GridControl.Columns>
</dxGrid:GridControl>
```

#Data Grouping  
The DevExpress Grid Control allows you to group data against the values displayed in its columns. Use the following code to group orders by date using the **GridColumn.IsGrouped** and **GridColumn.GroupInterval** properties.  

XAML  
```xml
<dxGrid:GridControl x:Name="grid" ItemsSource="{Binding Orders}" NewItemRowVisibility = "true"> 
    <dxGrid:GridControl.Columns> 
         <!-- ... --> 
         <dxGrid:DateColumn FieldName="Date" DisplayFormat="d" 
                            IsGrouped = "true" GroupInterval = "Date"/> 
         <!-- ... --> 
    </dxGrid:GridControl.Columns> 
</dxGrid:GridControl>
```

#Data Summaries  
The DevExpress Grid allows you to display total or group summaries – aggregate function values calculated against the entire dataset or record groups respectively - when data grouping is enabled.  

Total summaries are stored in the **GridControl.TotalSummaries** collection. Group summaries are stored in the **GridControl.GroupSummaries** collection. In both instances, individual summaries are specified by **GridColumnSummary** objects. To activate summary computations, you will need to specify the data field (**GridColumnSummary.FieldName**), aggregate function type (**GridColumnSummary.Type**) and summary value format (**GridColumnSummary.DisplayFormat**).  

Predefined aggregate function types are Count, Max, Min, Sum and Average.  

In this example, a group summary is used to display the maximum **Total** value for each record group, and a total summary to display the sum of all values in the **Total** column.  

The sample code below also illustrates the use of a custom defined aggregate function to count the number of "un-shipped" orders. Aggregate functions can be implemented by setting the **GridColumnSummary.Type** property to **Custom** and handling the **GridControl.CalculateCustomSummary** event.  

XAML  
```xml
<dxGrid:GridControl x:Name="grid" ItemsSource="{Binding Orders}" 
                    NewItemRowVisibility = "true" 
                    CalculateCustomSummary="OnCalculateCustomSummary"> 
    <!-- ... --> 
    <dxGrid:GridControl.GroupSummaries> 
        <dxGrid:GridColumnSummary FieldName="Total" Type="Max" 
                                  DisplayFormat="Max Total: {0:C0}"/> 
    </dxGrid:GridControl.GroupSummaries> 

      <dxGrid:GridControl.TotalSummaries> 
          <dxGrid:GridColumnSummary FieldName="Total" Type="Sum" 
                                    DisplayFormat= "Total: {0:C0}"/> 
          <dxGrid:GridColumnSummary FieldName="Shipped" Type="Custom" 
                                    DisplayFormat= "Not Shipped: {0}"/> 
      </dxGrid:GridControl.TotalSummaries> 
</dxGrid:GridControl>
```

C#  
```csharp
int count;
// ... 

void OnCalculateCustomSummary(object sender, CustomSummaryEventArgs e) {
    if (e.FieldName.ToString () == "Shipped")
        if (e.IsTotalSummary){
            if (e.SummaryProcess == CustomSummaryProcess.Start) {
                count = 0;
            }
            if (e.SummaryProcess == CustomSummaryProcess.Calculate) {
                if (!(bool)e.FieldValue)
                    count++;
                e.TotalValue = count;
            }
        }
}
```  

#Data Filtering  
The DevExpress Grid supports data filtering against multiple columns.  

To apply a filter against a specific column, use the **GridColumn.AutoFilterValue** property. To specify a comparison operator, use the **GridColumn.AutoFilterCondition** property.  

To activate filtering for end-users, enable the Grid’s built-in auto-filter panel using the **GridControl.AutoFilterPanelVisibility** property. Auto-filter functionality can be disabled for any column via the **GridColumn.AllowAutoFilter** property.  

XAML  
```xml
<dxGrid:GridControl x:Name="grid" ItemsSource="{Binding Orders}" 
                    NewItemRowVisibility = "true" 
                    CalculateCustomSummary="grid_CustomSummary" 
                    SortMode = "Multiple" AutoFilterPanelVisibility="true"> 
    <dxGrid:GridControl.Columns> 
         <!-- ... --> 
         <dxGrid:SwitchColumn FieldName="Shipped" AllowSort = "False" AllowAutoFilter="false"/> 
    </dxGrid:GridControl.Columns> 
</dxGrid:GridControl>
```

To create filter expressions that consists of multiple conditions applied to multiple columns, use the **GridControl.FilterExpression** and **GridControl.FilterString** properties as necessary.  

Once a filter has been applied, the DevExpress Grid automatically displays a filter panel at the bottom of its container. The panel provides feedback on the currently applied filter criteria and buttons designed to temporarily disable or to clear the filter. To control panel visibility, use the **GridControl.FilterPanelVisibility** property.  

#Result  
If you’ve followed this step-by-step tutorial, your resulting application should look like the following.  
![DevExpress Grid](https://www.devexpress.com//Products/Xamarin/Grid/XamarinStore/DevExpressGrid_GettingStarted_15_1_4.png) 