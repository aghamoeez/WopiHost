﻿using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WopiHost.Discovery.Enumerations;
using WopiHost.Url;

namespace WopiHost.Web.Controllers
{
	public class HomeController : Controller
	{
		public WopiUrlGenerator _urlGenerator;

		private IConfiguration Configuration { get; }

		public string WopiHostUrl => Configuration.GetValue("WopiHostUrl", string.Empty);

		public string WopiClientUrl => Configuration.GetValue("WopiClientUrl", string.Empty);

		public WopiUrlGenerator UrlGenerator
		{
			//TODO: remove test culture value and load it from configuration
			get { return _urlGenerator ?? (_urlGenerator = new WopiUrlGenerator(WopiClientUrl, new WopiUrlSettings { UI_LLCC = new CultureInfo("en-US") })); }
		}

		public HomeController(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public async Task<ActionResult> Index([FromQuery]string url)
		{
			//TODO: add proper access tokens

			if (string.IsNullOrEmpty(url))
			{
				//TODO: root folder id http://wopi.readthedocs.io/projects/wopirest/en/latest/ecosystem/GetRootContainer.html?highlight=EnumerateChildren (use ecosystem controller)
				string containerId = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(".\\")));
				url =  $"{WopiHostUrl}/wopi/containers/{containerId}/children?access_token=todo";
			}

			//todo: get the stuff from checkfileinfo
			dynamic data = await GetDataAsync(url);
			foreach (var file in data.ChildFiles)
			{
				string fileUrl = file.Url.ToString();
				var fileDetails = await GetDataAsync(fileUrl);
				file.EditUrl = await UrlGenerator.GetFileUrlAsync(fileDetails.FileExtension.ToString().TrimStart('.'), fileUrl, WopiActionEnum.Edit) +"&access_token=xyz";

			}

			//http://dotnet-stuff.com/tutorials/aspnet-mvc/how-to-render-different-layout-in-asp-net-mvc
			foreach (var container in data.ChildContainers)
			{
				//TODO create hierarchy
			}

			return View(data);
		}

		private async Task<dynamic> GetDataAsync(string url)
		{
			using (HttpClient client = new HttpClient())
			{
				using (Stream stream = await client.GetStreamAsync(url))
				{
					using (var sr = new StreamReader(stream))
					{
						using (var jsonTextReader = new JsonTextReader(sr))
						{
							var serializer = new JsonSerializer();
							return serializer.Deserialize(jsonTextReader);
						}
					}
				}
			}
		}
	}
}
