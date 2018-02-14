using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace MusicBeePlugin
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			ReloadListAsync();
		}

		string SelectedAddonId = string.Empty;
		List<AddonData> AddonList = new List<AddonData>();


		private string[] GetAddonDirectoryPaths()
		{
			string[] directoryPaths = {
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MusicBee\\Plugins"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MusicBee\\Skins"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"MusicBee\\Plugins"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"MusicBee\\Skins")
			};

			return directoryPaths;
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
					XmlNode ignoreUpdate = doc.DocumentElement.SelectSingleNode("ignore-update");

					if (addonId.InnerText.Length > 0)
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
									AddonList.Add(new AddonData()
									{
										Id = addonId.InnerText,
										Name = addonName.InnerText,
										AuthorName = addonData.membername,
										AvailableVersion = addonData.addon_version,
										Category = "Plugin",
										UpdateDate = addonData.update_date,
										UpdateStatus = UpdateStaus,
										UpdateState = "✖",
										IgnoreUpdate = (ignoreUpdate == null)? "false" : 
										(string.IsNullOrEmpty(ignoreUpdate.InnerText))? "false" : ignoreUpdate.InnerText

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
			AddonList.Clear();
			LoadAddonList();

			await Task.Run(() => ReloadInstalledAddonList());

			LoadAvailableUpdateList();
			LoadIgnoredAddonList();

			
		}

		private void LoadAddonList()
		{
			foreach (var addon in GetAllAddons())
			{
				addonListView.Items.Add(addon);
			}
		}

		private void LoadAvailableUpdateList()
		{
			foreach (var addon in AddonList)
			{
				if (addon.UpdateStatus == "✔")
				{
					updateAddonListView.Items.Add(addon);
				}
			}
		}

		private void LoadIgnoredAddonList()
		{
			foreach (var addon in AddonList)
			{
				if (addon.IgnoreUpdate == "true")
				{
					ignoredAddonListView.Items.Add(addon);
				}
			}
		}


		/// <summary>
		/// Get all the XML manifest file paths from plugin directory
		/// </summary>
		/// <returns>string[]</returns>
		private string[] GetAddonMetaFilePaths()
		{
			string[] metaFilePaths = { };

			foreach (var directory in GetAddonDirectoryPaths())
			{
				metaFilePaths = metaFilePaths.Concat(
					Directory.GetFiles(directory, "*.meta", SearchOption.AllDirectories)
					).ToArray();
			}

			return metaFilePaths;
		}


		private List<AddonData> GetAllAddons()
		{
			string[] filePaths = { };

			foreach (var directory in GetAddonDirectoryPaths())
			{
				filePaths = filePaths.Concat(
					Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories).Where(
						s => ((Path.GetFileName(s).StartsWith("mb_") || Path.GetFileName(s).StartsWith("MB_") || 
						Path.GetFileName(s).StartsWith("Mb_")) && s.EndsWith(".dll")) || 
						(s.EndsWith(".xml") && !s.Contains(".Json.")) || s.EndsWith(".xmlc")
						)
					).ToArray();
			}

			List<AddonData> addonList = new List<AddonData>();

			filePaths = filePaths.ToList().Select(c => 
			{
				if (File.Exists(c.Replace(".dll", ".meta").Replace(".xml", ".meta").Replace(".xmlc", ".meta"))) return c;

				string category = c.Contains("TheaterMode") ? "Theater Mode" :(c.Contains(".xml") || c.Contains(".xmlc") ? "Skin" : c.Contains(".dll") ? "Plugin" : "NA");
				string location = c;

				c = Path.GetFileName(c);
			
				addonList.Add(
					new AddonData()
					{
						Name = c.Replace("mb_", "").Replace("MB_", "").Replace("Mb_", "").Replace(".dll", "")
						.Replace(".xml", "").Replace(".xmlc", ""),

						Category = category,
						InstalledLocation = location
					}
					);

				return c;
			}).ToArray();


			return addonList;

		}

		private void AddonList_SelectionChange(object sender, System.Windows.Controls.SelectionChangedEventArgs e, ListView v)
		{
			bool ItemSelected = (((ListView)e.Source).SelectedIndex < 0 ? false : true);
			if ((AddonData)v.SelectedValue == null) return;

			if (ItemSelected && ((AddonData)v.SelectedValue).UpdateStatus != "✖")
			{
				SelectedAddonId = ((AddonData)v.SelectedValue).Id;
				DownloadBtn.IsEnabled = true;
			}
			else
			{
				SelectedAddonId = string.Empty;
				DownloadBtn.IsEnabled = false;
			}
		}

		private void DownloadBtn_Click(object sender, RoutedEventArgs e)
		{
			if(!string.IsNullOrEmpty(SelectedAddonId))
			{
				string AddonUrl = string.Format("https://getmusicbee.com/addons/{0}/", SelectedAddonId);
				Process.Start(AddonUrl);
			}
		}

		private void ignoredAddonListView_SelectionChange(object sender, SelectionChangedEventArgs e)
		{
			AddonList_SelectionChange(sender, e, ignoredAddonListView);
		}

		private void updateAddonListView_SelectionChange(object sender, SelectionChangedEventArgs e)
		{
			AddonList_SelectionChange(sender, e, updateAddonListView);
		}

		private void addonListView_SelectionChange(object sender, SelectionChangedEventArgs e)
		{
			AddonList_SelectionChange(sender, e, addonListView);
		}

		private void TabSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.Source is TabControl)
			{
				DownloadBtn.IsEnabled = false;

				ignoredAddonListView.SelectedIndex = -1;
				updateAddonListView.SelectedIndex = -1;
				addonListView.SelectedIndex = -1;
			}
		}
	}
}
