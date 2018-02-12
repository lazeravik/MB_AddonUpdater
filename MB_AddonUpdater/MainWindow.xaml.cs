using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace MusicBeePlugin
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			//Reload the list on startup
			ReloadListAsync();
		}



		private Task ReloadInstalledAddonList()
		{
			string[] mbAddonMetaFilePaths = GetAddonMetaFilePaths();

			if(mbAddonMetaFilePaths.Length > 0)
			{
				List<StoredData> storedData = new List<StoredData>();
				foreach (var metaFilePath in mbAddonMetaFilePaths)
				{
					XmlDocument doc = new XmlDocument();
					doc.Load(metaFilePath);
					XmlNode addonName = doc.DocumentElement.SelectSingleNode("name");
					XmlNode addonId = doc.DocumentElement.SelectSingleNode("id");
					XmlNode addonVersion = doc.DocumentElement.SelectSingleNode("version");

					if(addonId.InnerText.Length > 0)
					{
						using (WebClient client = new WebClient())
						{
							string jsonString = client.DownloadString(
								string.Format("https://getmusicbee.com/api/1.0/?type=json&action=addon-info&id={0}", addonId.InnerText));

							if(!string.IsNullOrEmpty(jsonString))
							{
								dynamic addonData = JsonConvert.DeserializeObject(jsonString);
								string UpdateStaus = (addonData.addon_version > addonVersion.InnerText) ? "✔" : "✖";
								Dispatcher.Invoke(() =>
								{
									addonListView.Items.Add(
									new AddonData()
									{
										Id = addonId.InnerText,
										Name = addonName.InnerText,
										AuthorName = addonData.membername,
										AvailableVersion = addonData.addon_version,
										Category = "Plugin",
										UpdateDate = addonData.update_date,
										UpdateStatus = UpdateStaus,
										UpdateState = "✖"
									});
								});
							}
						}
					}
				}
			}

			return Task.FromResult(0);
		}

		private async void ReloadListAsync()
		{
			await Task.Run(() => ReloadInstalledAddonList());
		}

		private void reloadListBtn_Click(object sender, RoutedEventArgs e)
		{
			ReloadListAsync();
		}


		
		/// <summary>
		/// Get all the XML manifest file paths from plugin directory
		/// </summary>
		/// <returns>string[]</returns>
		private string[] GetAddonMetaFilePaths()
		{
			var mbAddonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"MusicBee/Plugins");
			return Directory.GetFiles(mbAddonPath, "mb_*.meta", SearchOption.AllDirectories);
		}
	}
}
