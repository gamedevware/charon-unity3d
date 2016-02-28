using System;
using System.Collections;
using System.Linq;
using System.Text;
using Assets.Unity.Charon.Editor.Json;
using Assets.Unity.Charon.Editor.Tasks;
using Assets.Unity.Charon.Editor.Models;

namespace Assets.Unity.Charon.Editor
{
	public static class Licenses
	{
		public static Coroutine<LicenseInfo> GetLicense(bool scheduleCoroutine)
		{
			var stateMachine = GetLicenseAsync();

			return scheduleCoroutine ? CoroutineScheduler.Schedule<LicenseInfo>(stateMachine) : new Coroutine<LicenseInfo>(stateMachine);
		}
		private static IEnumerable GetLicenseAsync()
		{
			var getLicenses = GetLicenses(scheduleCoroutine: false);
			yield return getLicenses.IgnoreFault();

			if (getLicenses.HasErrors)
				yield break;
			var licenses = getLicenses.GetResult();
			if (licenses == null || licenses.Length == 0)
				yield break;

			var selectedLicense = licenses.FirstOrDefault(l => string.Equals(l.SerialNumber, Settings.Current.SelectedLicense, StringComparison.OrdinalIgnoreCase));
			if (selectedLicense == null)
				selectedLicense = licenses.FirstOrDefault();
			yield return selectedLicense;
		}

		public static Coroutine<LicenseInfo[]> GetLicenses(bool scheduleCoroutine)
		{
			var stateMachine = GetLicensesAsync();

			return scheduleCoroutine ? CoroutineScheduler.Schedule<LicenseInfo[]>(stateMachine) : new Coroutine<LicenseInfo[]>(stateMachine);
		}
		private static IEnumerable GetLicensesAsync()
		{
			var licensesJson = new StringBuilder();
			var errorJson = new StringBuilder();
			var checkLicense = new ExecuteCommandTask(
				Settings.Current.ToolsPath,
				(s, ea) => { licensesJson.Append(ea.Data ?? ""); },
				(s, ea) => { errorJson.Append(ea.Data ?? ""); },
				"ACCOUNT", "LICENSE", "SHOWLOCAL",
				"--noPrompt",
				"--verbose"
			);

			checkLicense.StartInfo.EnvironmentVariables["CHARON_APP_DATA"] = Settings.GetAppDataPath();
			if (string.IsNullOrEmpty(Settings.Current.LicenseServerAddress) == false)
				checkLicense.StartInfo.EnvironmentVariables["CHARON_LICENSE_SERVER"] = Settings.Current.LicenseServerAddress;
			checkLicense.RequireDotNetRuntime();
			checkLicense.Start();

			yield return checkLicense.IgnoreFault();
			if (errorJson.Length > 0)
				throw new InvalidOperationException(errorJson.ToString());

			var licensesArray = (JsonArray)JsonValue.Parse(licensesJson.ToString());
			var licenses = licensesArray.As<LicenseInfo[]>();
			yield return licenses;
		}

		public static Coroutine<LicenseInfo[]> DownloadLicenses(string email, string password, bool scheduleCoroutine)
		{
			if (email == null) throw new ArgumentNullException("email");
			if (password == null) throw new ArgumentNullException("password");

			var stateMachine = DownloadLicensesAsync(email, password);
			return scheduleCoroutine ? CoroutineScheduler.Schedule<LicenseInfo[]>(stateMachine) : new Coroutine<LicenseInfo[]>(stateMachine);
		}
		private static IEnumerable DownloadLicensesAsync(string email, string password)
		{
			var errorJson = new StringBuilder();
			var checkLicense = new ExecuteCommandTask(
				Settings.Current.ToolsPath,
				null,
				(s, ea) => { errorJson.Append(ea.Data ?? ""); },
				"ACCOUNT", "LICENSE", "DOWNLOAD",
				"--credentials", email, password,
				"--noPrompt",
				"--verbose"
			);

			checkLicense.StartInfo.EnvironmentVariables["CHARON_APP_DATA"] = Settings.GetAppDataPath();
			if (string.IsNullOrEmpty(Settings.Current.LicenseServerAddress) == false)
				checkLicense.StartInfo.EnvironmentVariables["CHARON_LICENSE_SERVER"] = Settings.Current.LicenseServerAddress;
			checkLicense.RequireDotNetRuntime();
			checkLicense.Start();

			yield return checkLicense.IgnoreFault();

			if (errorJson.Length > 0)
				throw new InvalidOperationException(errorJson.ToString());

			foreach (var step in GetLicensesAsync())
				yield return step;
		}

		public static Coroutine<LicenseInfo[]> Register(string firstName, string lastName, string organizationName, string email, string password, string unityInvoiceNumber, bool scheduleCoroutine)
		{
			if (firstName == null) throw new ArgumentNullException("firstName");
			if (lastName == null) throw new ArgumentNullException("lastName");
			if (organizationName == null) throw new ArgumentNullException("organizationName");
			if (email == null) throw new ArgumentNullException("email");
			if (password == null) throw new ArgumentNullException("password");
			if (unityInvoiceNumber == null) throw new ArgumentNullException("unityInvoiceNumber");

			var stateMachine = RegisterAsync(firstName, lastName, organizationName, email, password, unityInvoiceNumber);
			return scheduleCoroutine ? CoroutineScheduler.Schedule<LicenseInfo[]>(stateMachine) : new Coroutine<LicenseInfo[]>(stateMachine);
		}
		private static IEnumerable RegisterAsync(string firstName, string lastName, string organizationName, string email, string password, string unityInvoiceNumber)
		{
			var errorJson = new StringBuilder();
			var checkLicense = new ExecuteCommandTask(
				Settings.Current.ToolsPath,
				null,
				(s, ea) => { errorJson.Append(ea.Data ?? ""); },
				"ACCOUNT", "REGISTER",
				"--firstName", firstName,
				"--lastName", lastName,
				"--organizationName", organizationName,
				"--email", email,
				"--password", password,
				"--unityInvoiceNumber", unityInvoiceNumber,
				"--noPrompt",
				"--verbose"
			);

			checkLicense.StartInfo.EnvironmentVariables["CHARON_APP_DATA"] = Settings.GetAppDataPath();
			if (string.IsNullOrEmpty(Settings.Current.LicenseServerAddress) == false)
				checkLicense.StartInfo.EnvironmentVariables["CHARON_LICENSE_SERVER"] = Settings.Current.LicenseServerAddress;
			checkLicense.RequireDotNetRuntime();
			checkLicense.Start();

			yield return checkLicense.IgnoreFault();

			if (errorJson.Length > 0)
				throw new InvalidOperationException(errorJson.ToString());

			foreach (var step in DownloadLicensesAsync(email, password))
				yield return step;
		}
	}
}
