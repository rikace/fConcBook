using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using DevExpress.Mobile.Core;
using DevExpress.Mobile.DataGrid.Localization;
using DevExpress.Mobile.DataGrid.Theme;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public class DemoContentPage : TabbedPage {
        const string DescriptionIcon = "page_description.png";
        const string ScharpIcon = "page_csharp.png";
        const string XamlIcon = "page_xaml.png";
        const string DemoIcon = "page_demo.png";
        

        const string PageDemoName = "Demo";
        const string PageDescriptionName = "Description";
        const string PageCsharpName = "C# Code";
        const string PageXamlName = "Xaml";

        Page page;
        
        protected override void OnBindingContextChanged() {
            base.OnBindingContextChanged();
            if (Device.OS == TargetPlatform.WinPhone)
                Device.BeginInvokeOnMainThread(UpdateContent);
            else
                UpdateContent();
        }
        void UpdateContent() {
			try {
				SwitchToSystemCulture();
			} catch { }
            DemoInfo info = BindingContext as DemoInfo;
            if (info == null) {
                ClearChildren();
                Title = String.Empty;
                return;
            }
			//ClearChildren();
            
            if (page != null && page is ContentPage)
                ((ContentPage)page).Content = null;

            page = info.CreatePage();
            AssignPageTitle(page, PageDemoName);
            page.Icon = LoadPageIcon(DemoIcon);
            /*
            ContentPage waitPage = new ContentPage();
            waitPage.InputTransparent = true;
            waitPage.BackgroundColor = Color.FromRgba(0, 0, 0, 64);
            Grid g = new Grid();
            g.Children.Add(new ActivityIndicator() { IsRunning = true, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center });
            waitPage.Content = g;

            await Navigation.PushModalAsync(waitPage, false);

            await System.Threading.Tasks.Task.Delay(1000);
            */
            List<Page> pages = new List<Page>();
            AddPage(pages, page);
            if (!info.HideAdditionalPages) {
                AddPage(pages, CreateDescriptionPage(page, info));
                AddPage(pages, CreateCSharpCodePage(page));
                AddPage(pages, CreateXamlCodePage(page));
            }
            ReplacePages(pages);
            
            //ISupportsStartFinish startFinish = page as ISupportsStartFinish;
            //if (startFinish != null)
            //    startFinish.Start();

            //await Navigation.PopModalAsync(false);
        }

        void ReplacePages(List<Page> pages) {
            if (Device.OS != TargetPlatform.WinPhone) {
                ClearChildren();
                foreach (Page page in pages)
                    Children.Add(page);
                CurrentPage = pages[0];
                return;
            }


            //WinPhone specific. ClearChildren leads to incorrect behavoir
            int count = pages.Count;
            for (int i = 0; i < count; i++) {
                if (i < Children.Count)
                    ReplaceChild(i, pages[i]);
                else
                    Children.Add(pages[i]);
            }
            for (int i = Children.Count - 1; i >= count; i--)
                RemoveChild(i);
            CurrentPage = pages[0];
        }

        void ReplaceChild(int index, Page page) {
            Page child = Children[index];
            DetachChild(child);
            Children.RemoveAt(index);
            Children.Insert(index, page);
        }

        void RemoveChild(int index) {
            Page child = Children[index];
            DetachChild(child);
            Children.RemoveAt(index);
        }

        void SwitchToSystemCulture() {
            IGlobalizationService service = GlobalServices.Instance.GetService<IGlobalizationService>();
            if (service == null)
                return;

            CultureInfo culture = service.CurrentOSCulture;
            service.CurrentCulture = culture;
            service.CurrentUICulture = culture;
            GridLocalizer.ResetCache();
            DemoLocalizer.ResetCache();
        }

        void ClearChildren() {
            foreach (object child in Children)
                DetachChild(child);
            Children.Clear();
        }
        void DetachChild(object child) {
            ISupportsStartFinish startFinish = child as ISupportsStartFinish;
            if (startFinish != null)
                startFinish.Finish();
        }

        void AddPage(List<Page> pages, Page page) {
            if (page != null)
                pages.Add(page);
        }

        void AddChild(Page page) {
            if (page != null) {
                Children.Add(page);
            }
        }

        Page CreateDescriptionPage(Page demoPage, DemoInfo info) {
            if (info == null)
                return null;

            HtmlViewerPage result = new HtmlViewerPage();
            AssignPageTitle(result, PageDescriptionName);
            result.ObtainSourceDelegate = () => {
                string htmlString = LoadDescriptionHtml(demoPage, ".html");
                if (String.IsNullOrEmpty(htmlString))
                    return null;
                return new HtmlWebViewSource() { Html = ProcessHtmlString(htmlString) };
            };
            result.Icon = LoadPageIcon(DescriptionIcon);
            return result;
        }
        readonly Dictionary<string, string> defaultCss = new Dictionary<string, string>();
        string ProcessHtmlString(string html) {
            string css;
            if (!defaultCss.TryGetValue(ThemeManager.ThemeName, out css)) {
                string cssFileName = ThemeManager.ThemeName == Themes.Dark ? "default_dark.css" : "default.css";
                css = LoadResourceText("DevExpress.GridDemo.Descriptions." + cssFileName, this.GetType().GetTypeInfo().Assembly);
                css = ProcessCss(css);
                css = @"<style type=""text/css"">" + css + "</style>";
                defaultCss[ThemeManager.ThemeName] = css;
            }
            if (String.IsNullOrEmpty(css))
                return html;
            return html.Replace(@"<link href=""default.css"" rel=""stylesheet"" type=""text/css"">", css);
        }
        readonly Dictionary<string, string> defaultCodeCss = new Dictionary<string, string>();
        string ProcessCodeHtmlString(string html) {
            string css;
            if (!defaultCodeCss.TryGetValue(ThemeManager.ThemeName, out css)) {
                string cssFileName = ThemeManager.ThemeName == Themes.Dark ? "highlight_dark.css" : "highlight.css";
                css = LoadResourceText("DevExpress.GridDemo.Descriptions." + cssFileName, this.GetType().GetTypeInfo().Assembly);
                css = ProcessCss(css);
                css = @"<style type=""text/css"">" + css + "</style>";
                defaultCodeCss[ThemeManager.ThemeName] = css;
            }
            if (String.IsNullOrEmpty(css))
                return html;
            return html.Replace(@"<link rel=""stylesheet"" type=""text/css"" href=""highlight.css"">", css);
        }

        string ProcessCss(string css) {
            if (Device.OS != TargetPlatform.WinPhone)
                return css;

            css = "@-ms-viewport{width:device-width; height:device-height;}\r\n" + css;

            return css;
        }
        Page CreateCSharpCodePage(Page demoPage) {
            HtmlViewerPage result = new HtmlViewerPage();
            AssignPageTitle(result, PageCsharpName);
            result.ObtainSourceDelegate = () => {
                string htmlString = LoadResourceText(demoPage, ".xaml.code.htm");
                if (String.IsNullOrEmpty(htmlString))
                    return null;
                return new HtmlWebViewSource() { Html = ProcessCodeHtmlString(htmlString) };
            };
            result.Icon = LoadPageIcon(ScharpIcon);
            return result;
        }
        Page CreateXamlCodePage(Page demoPage) {
            HtmlViewerPage result = new HtmlViewerPage();
            AssignPageTitle(result, PageXamlName);
            result.ObtainSourceDelegate = () => {
                string htmlString = LoadResourceText(demoPage, ".xaml.htm");
                if (String.IsNullOrEmpty(htmlString))
                    return null;
                return new HtmlWebViewSource() { Html = ProcessCodeHtmlString(htmlString) };
            };
            result.Icon = LoadPageIcon(XamlIcon);
            return result;
        }

        void AssignPageTitle(Page page, string title) {
            if (Device.OS != TargetPlatform.Android)
                page.Title = title;
        }

        string LoadResourceText(Page demoPage, string prefix, string suffix) {
            string className = demoPage.GetType().Name;
            Assembly asm = demoPage.GetType().GetTypeInfo().Assembly;
            return LoadResourceText(prefix + className + suffix, asm);
        }
        string LoadResourceText(string resourceName, Assembly assembly) {
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;

            using (StreamReader reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
        string LoadResourceText(Page demoPage, string suffix) {
            return LoadResourceText(demoPage, "DevExpress.GridDemo.Sources.", suffix);
        }
        string LoadDescriptionHtml(Page demoPage, string suffix) {
            return LoadResourceText(demoPage, "DevExpress.GridDemo.Descriptions.", suffix);
        }

        FileImageSource LoadPageIcon(string resourceName) {
            return resourceName;
        }
    }

    public interface ISupportsStartFinish {
        void Start();
        void Finish();
    }
}