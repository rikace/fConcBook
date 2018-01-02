#define REACTIVE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Json;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace ReactiveExtensionsGithubClientSample
{
	[Activity (Label = "GithubApiClientSample", MainLauncher = true)]
	public class MainActivity : ListActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			
			var baseDir = Path.Combine (Application.ApplicationInfo.DataDir, "image_cache");
			if (!Directory.Exists (baseDir))
				Directory.CreateDirectory (baseDir);
			
			var data = new List<IDictionary<string,object>> ();
			var urls = new Dictionary<Uri,List<string>> ();
			var getUrl = new Uri ("https://api.github.com/users/mono/repos");
			var hr = new HttpWebRequest (getUrl);
			var req = Observable.FromAsyncPattern<WebResponse> (hr.BeginGetResponse, hr.EndGetResponse);
			Observable.Defer (req).Subscribe (v => {
				var json = (IEnumerable<JsonValue>) JsonValue.Load (v.GetResponseStream ());
				json.Cast<JsonObject> ().ToList ().ForEach (item => {
					var uri = new Uri ((string) ((JsonObject) item ["owner"])["avatar_url"]);
					var file = Path.Combine (baseDir, ((int) item ["id"]) + new FileInfo (uri.LocalPath).Extension);
					if (!urls.ContainsKey (uri))
						urls.Add (uri, new List<string> () {file});
					else
						urls [uri].Add (file);
					data.Add (new JavaDictionary<string,object> () { {"Text", item ["description"]}, {"Name", item ["name"]}, {"Icon", Path.Combine (baseDir, file) }});
					urls.ToList ().ForEach (p => {
						var iwc = new WebClient ();
						iwc.DownloadDataCompleted += (isender, ie) => p.Value.ForEach (s => {
							if (ie.Result != null)
								using (var fs = File.Create (s))
									fs.Write (ie.Result, 0, ie.Result.Length);
						});
						iwc.DownloadDataAsync (p.Key);
					});
				});

				var from = new string [] {"Text", "Name", "Icon"};
				var to = new int [] { Resource.Id.textMessage, Resource.Id.textName, Resource.Id.iconView};
					
				this.RunOnUiThread (() => {
					ListAdapter = new SimpleAdapter (this, data, Resource.Layout.ListItem, from, to);
				});
			});
		}
	}
}
