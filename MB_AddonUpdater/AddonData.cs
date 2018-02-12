using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBeePlugin
{
	class AddonData
	{
		public string Name { get; set; }
		public string AuthorName { get; set; }
		public string Id { get; set; }
		public string Category { get; set; }
		public string UpdateDate { get; set; }
		public string UpdateStatus { get; set; }
		public string UpdateState { get; set; }

		public string AvailableVersion { get; set; }
	}
}
