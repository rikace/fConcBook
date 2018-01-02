using System;
using System.Collections.Generic;
using Xamarin.Forms;
using DevExpress.Mobile.DataGrid;
using System.Collections.ObjectModel;
using System.Reflection;

namespace DevExpress.GridDemo {
	public class App : Application {
        static readonly ObservableCollection<DemoGroup> demos = CreateDemos();

        static ObservableCollection<DemoGroup> CreateDemos() {
            ObservableCollection<DemoGroup> groups = new ObservableCollection<DemoGroup>();
            AddGroup(groups, CreateDataDemos());
            AddGroup(groups, CreateFilterDemos());
            AddGroup(groups, CreateSortDemos());
            AddGroup(groups, CreateGroupDemos());
            AddGroup(groups, CreateSummaryDemos());
            AddGroup(groups, CreateCustomizationDemos());
            AddGroup(groups, CreateOtherDemos());
            return groups;
        }
        static void AddGroup(ObservableCollection<DemoGroup> groups, DemoGroup group) {
            if (group != null)
                groups.Add(group);
        }
        static DemoGroup CreateSortDemos() {
            //ObservableCollection<DemoInfo> result = new ObservableCollection<DemoInfo>();
            DemoGroup group = new DemoGroup();
            group.Title = "Sort";
            group.ShortTitle = "Sort";
            ObservableCollection<DemoInfo> result = group;
            result.Add(new DemoInfo() {
                Title = "Sort",
                CreatePage = () => { return new SortPage(); },
				ShortDescription = "Shows how to sort grid data by a column.",
            });

            result.Add(new DemoInfo() {
                Title = "Sort by Multiple Columns",
                CreatePage = () => { return new SortMultiColumnPage(); },
				ShortDescription = "Shows how to enable sorting grid data by multiple columns.",
            });
            return group;
        }
        static DemoGroup CreateGroupDemos() {
            DemoGroup group = new DemoGroup();
            group.Title = "Group";
            group.ShortTitle = "Grp";
            ObservableCollection<DemoInfo> result = group;
            result.Add(new DemoInfo() {
                Title = "Group",
                CreatePage = () => { return new GroupPage(); },
				ShortDescription = "Shows how to group data rows by values in the specified column.",
            });

            result.Add(new DemoInfo() {
                Title = "Group Dates",
                CreatePage = () => { return new GroupDatesPage(); },
				ShortDescription = "Shows how to group data by specific date intervals.",
            });
            return group;
        }
        static DemoGroup CreateSummaryDemos() {
            DemoGroup group = new DemoGroup();
            group.Title = "Summary";
            group.ShortTitle = "Sum";
            ObservableCollection<DemoInfo> result = group;
            result.Add(new DemoInfo() {
                Title = "Total Summary",
                CreatePage = () => { return new SummaryTotalPage(); },
				ShortDescription = "Shows how to calculate summaries against all column values.",
            });

            result.Add(new DemoInfo() {
                Title = "Group Summary",
                CreatePage = () => { return new SummaryGroupPage(); },
				ShortDescription = "Shows how to calculate summaries against column values within groups.",
            });

            result.Add(new DemoInfo() {
                Title = "Custom Summary",
                CreatePage = () => { return new SummaryCustomPage(); },
				ShortDescription = "Shows how to set a custom rule to calculate a data summary.",
            });
            return group;
        }
        static DemoGroup CreateFilterDemos() {
            DemoGroup group = new DemoGroup();
            group.Title = "Filter";
            group.ShortTitle = "Filter";
            ObservableCollection<DemoInfo> result = group;

            result.Add(new DemoInfo() {
                Title = "Filter",
                CreatePage = () => { return new FilterPage(); },
				ShortDescription = "Shows how to specify a filter expression to be applied in the grid.",
            });

            result.Add(new DemoInfo() {
                Title = "Auto Filter",
                CreatePage = () => { return new FilterAutoFilterPage(); },
				ShortDescription = "Shows how to display an auto-filter panel in the grid allowing end-users to filter data on the fly by typing text into the panel cells.",
            });
            return group;
        }
        static DemoGroup CreateDataDemos() {
            DemoGroup group = new DemoGroup();
            group.Title = "Data";
            group.ShortTitle = "Data";
            ObservableCollection<DemoInfo> result = group;

            result.Add(new DemoInfo() {
                Title = "First Look",
                CreatePage = () => { return new FirstLookPage(); },
                ShortDescription = "TODO:",
            });

            result.Add(new DemoInfo() {
                Title = "Auto Generate Columns",
                CreatePage = () => { return new ColumnsAutoGeneratePage(); },
				ShortDescription = "Shows the DXGrid's built-in capability to automatically generate columns based on the bound data source.",
            });

            result.Add(new DemoInfo() {
                Title = "New Item Row",
                CreatePage = () => { return new NewItemRowPage(); },
				ShortDescription = "Shows how to provide end-users with the capability to add new data records.",
            });

            result.Add(new DemoInfo() {
                Title = "Pull To Refresh",
                CreatePage = () => { return new PullToRefreshPage(); },
				ShortDescription = "Shows how to refresh the grid and synchronize the data it displays with information in the bound data source.",
            });

            result.Add(new DemoInfo() {
                Title = "Load More",
                CreatePage = () => { return new LoadMorePage(); }
            });

            result.Add(new DemoInfo() {
                Title = "Unbound Columns",
                CreatePage = () => { return new UnboundColumnsPage(); },
				ShortDescription = "Shows how to create grid columns that are not bound to any field of a data source and populate them manually.",
            });

			result.Add(new DemoInfo() {
				Title = "Horizontal Scrolling",
				CreatePage = () => { return new HorizontalScrollingPage(); },
				ShortDescription = "TODO:",
			});

            return group;
        }

        static DemoGroup CreateOtherDemos() {
            DemoGroup group = new DemoGroup();
            group.Title = "Other";
            group.ShortTitle = "Oth";
            ObservableCollection<DemoInfo> result = group;

            result.Add(new DemoInfo() {
                Title = "Export",
                CreatePage = () => { return new ExportPage(); },
                ShortDescription = "",
            });

            result.Add(new DemoInfo() {
                Title = "Save/Load State",
                CreatePage = () => { return new SaveLoadStatePage(); },
                ShortDescription = "",
            });

            result.Add(new DemoInfo() {
                Title = "Real-Time Data",
                CreatePage = () => { return new RealTimeDataPage(); },
                ShortDescription = "",
            });

            result.Add(new DemoInfo()
            {
                Title = "Search Highlight",
                CreatePage = () => { return new SearchHighlightCellPage(); },
                ShortDescription = "SearchHighlightCellPage.",
            });

            result.Add(new DemoInfo() {
                Title = "Large Data Source",
                CreatePage = () => { return new LargeDataSourcePage(); },
                ShortDescription = "Large Data Source Page.",
            });

            result.Add(new DemoInfo() {
                Title = "Localization",
                CreatePage = () => { return new LocalizationPage(); },
                ShortDescription = "Localization Page.",
            });

            result.Add(new DemoInfo() {
                Title = "About",
                CreatePage = () => { return new AboutPage(); },
                ShortDescription = "About Page.",
                HideAdditionalPages = true,
            });


            //result.Add(new DemoInfo() {
            //    Title = "Test",
            //    CreatePage = () => { return new TestPage(); }
            //});
            return group;
        }
        static DemoGroup CreateCustomizationDemos() {
            DemoGroup group = new DemoGroup();
            group.Title = "Customization";
            group.ShortTitle = "Cust";
            ObservableCollection<DemoInfo> result = group;
            
            result.Add(new DemoInfo() {
                Title = "Cell Template",
                CreatePage = () => { return new CellTemplatePage(); },
                ShortDescription = "",
            });

            result.Add(new DemoInfo() {
                Title = "User Restrictions",
                CreatePage = () => { return new RestrictionsPage(); },
                ShortDescription = "Shows how to set end-user restrictions.",
            });

            result.Add(new DemoInfo() {
                Title = "Themes",
                CreatePage = () => { return new ThemesPage(); },
				ShortDescription = "Shows how to apply themes to the grid.",
            });

            result.Add(new DemoInfo() {
                Title = "Customize Cell",
                CreatePage = () => { return new CustomizeCellPage(); },
				ShortDescription = "Shows how to customize grid cell appearance.",
            });

            result.Add(new DemoInfo() {
                Title = "Swipe Buttons",
                CreatePage = () => { return new SwipeButtonsPage(); },
                ShortDescription = "Shows how to extend the grid’s UI with additional buttons that appear when swiping left to right, or right to left, over a data row.",
            });
            result.Add(new DemoInfo() {
                Title = "Column Chooser",
                CreatePage = () => { return new ColumnChooserPage(); },
                ShortDescription = "",
            });
            result.Add(new DemoInfo() {
                Title = "Row Details",
                CreatePage = () => { return new RowDetailsPage(); },
                ShortDescription = "",
            });
			result.Add(new DemoInfo() {
				Title = "Popup Menu Customization",
				CreatePage = () => { return new MenuCustomizationPage(); },
				ShortDescription = "",
			});
            result.Add(new DemoInfo() {
                Title = "Conditional Formatting",
                CreatePage = () => { return new ConditionalFormattingPage(); },
                ShortDescription = "",
            });
            
            result.Add(new DemoInfo() {
                Title = "Row Edit Mode",
                CreatePage = () => { return new RowEditModePage(); },
                ShortDescription = "",
            });

            return group;
        }
        public static Page GetMainPage() {
            MainPage demoList = new MainPage();
            demoList.DemoList = demos;
			return demoList;
		}

        public App() {
            this.MainPage = GetMainPage();
        }
	}

    public class DemoGroup : ObservableCollection<DemoInfo> {
        public string Title { get; set; }
        public string ShortTitle { get; set; }
    }

    public class DemoInfo : BindableObject { 
		public static readonly BindableProperty TitleProperty = BindableProperty.Create("Title", typeof(string), typeof(DemoInfo), default(string));
        public string Title {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
		public static readonly BindableProperty ShortDescriptionProperty = BindableProperty.Create("ShortDescription", typeof(string), typeof(DemoInfo), default(string));
        public string ShortDescription {
            get { return (string)GetValue(ShortDescriptionProperty); }
            set { SetValue(ShortDescriptionProperty, value); }
        }
		public static readonly BindableProperty HideAdditionalPagesProperty = BindableProperty.Create("HideAdditionalPages", typeof(bool), typeof(DemoInfo) , default(bool));
        public bool HideAdditionalPages {
            get { return (bool)GetValue(HideAdditionalPagesProperty); }
            set { SetValue(HideAdditionalPagesProperty, value); }
        }
        //public static readonly BindableProperty DescriptionProperty = BindableProperty.Create<DemoInfo, string>(o => o.Description, default(string));
        //public string Description {
        //    get { return (string)GetValue(DescriptionProperty); }
        //    set { SetValue(DescriptionProperty, value); }
        //}
		public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create("IsVisible", typeof(bool), typeof(DemoInfo) , true);
        public bool IsVisible {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }
		public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create("IsSelected", typeof(bool), typeof(DemoInfo), false);
        public bool IsSelected {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public DemoInfo() {
        }

        public Func<Page> CreatePage { get; set; }
        //public Page Page { get; set; }
    }
}

