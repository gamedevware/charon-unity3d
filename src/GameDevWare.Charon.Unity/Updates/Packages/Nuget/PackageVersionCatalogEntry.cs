/*
	Copyright (c) 2023 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
*/

using System;
using GameDevWare.Charon.Unity.Json;
using GameDevWare.Charon.Unity.Utils;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Updates.Packages.Nuget
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal sealed class PackageVersionCatalogEntry
	{
		[JsonMember("title")]
		public string Title;
		[JsonMember("summary")]
		public string Summary;
		[JsonMember("description")]
		public string Description;
		[JsonMember("listed")]
		public bool IsListed = true; // github doesn't have this parameter, so all packages are listed
		[JsonMember("version")]
		public SemanticVersion Version;
		[JsonMember("requireLicenseAcceptance")]
		public bool IsRequireLicenseAcceptance;
		[JsonMember("licenseUrl")]
		public string LicenseUrl;
		[JsonMember("packageContent")]
		public string PackageContent;
		[JsonMember("licenseExpression")]
		public string LicenseExpression;
		[JsonMember("published")]
		public DateTime PublishedAt;
	}
}