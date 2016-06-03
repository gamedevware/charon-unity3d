using System;
using System.Collections;
using System.Linq;
using System.Text;
using Assets.Editor.GameDevWare.Charon.Json;
using Assets.Editor.GameDevWare.Charon.Models;
using Assets.Editor.GameDevWare.Charon.Tasks;
using Assets.Editor.GameDevWare.Charon.Utils;

namespace Assets.Editor.GameDevWare.Charon
{
	public static class Licenses
	{
		public static Promise<LicenseInfo> GetLicense(bool scheduleCoroutine)
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

		public static Promise<LicenseInfo[]> GetLicenses(bool scheduleCoroutine)
		{
			var stateMachine = GetLicensesAsync();

			return scheduleCoroutine ? CoroutineScheduler.Schedule<LicenseInfo[]>(stateMachine) : new Coroutine<LicenseInfo[]>(stateMachine);
		}
		private static IEnumerable GetLicensesAsync()
		{
			var checkLicenseAsync = ToolsRunner.RunCharonAsTool(
				"ACCOUNT", "LICENSE", "SHOWLOCAL",
				"--noPrompt",
				"--verbose"
			);
			yield return checkLicenseAsync;

			var errorData = checkLicenseAsync.GetResult().GetErrorData();
			if (string.IsNullOrEmpty(errorData) == false)
				throw new InvalidOperationException(errorData);

			var licensesArray = (JsonArray)JsonValue.Parse(checkLicenseAsync.GetResult().GetOutputData());
			var licenses = licensesArray.As<LicenseInfo[]>();
			yield return licenses;
		}

		public static Promise<LicenseInfo[]> DownloadLicenses(string email, string password, bool scheduleCoroutine)
		{
			if (email == null) throw new ArgumentNullException("email");
			if (password == null) throw new ArgumentNullException("password");

			var stateMachine = DownloadLicensesAsync(email, password);
			return scheduleCoroutine ? CoroutineScheduler.Schedule<LicenseInfo[]>(stateMachine) : new Coroutine<LicenseInfo[]>(stateMachine);
		}
		private static IEnumerable DownloadLicensesAsync(string email, string password)
		{
			var checkLicenseAsync = ToolsRunner.RunCharonAsTool(
				"ACCOUNT", "INITIALIZE",
				"--email", email,
				"--password", password,
				"--noPrompt",
				"--verbose"
			);
			yield return checkLicenseAsync;

			var errorData = checkLicenseAsync.GetResult().GetErrorData();
			if (string.IsNullOrEmpty(errorData) == false)
				throw new InvalidOperationException(errorData);

			foreach (var step in GetLicensesAsync())
				yield return step;
		}

		public static Promise<LicenseInfo[]> Register(string firstName, string lastName, string organizationName, string email, string password, string unityInvoiceNumber, bool scheduleCoroutine)
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
			var registerAsync = ToolsRunner.RunCharonAsTool(
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


			yield return registerAsync;

			var errorData = registerAsync.GetResult().GetErrorData();
			if (string.IsNullOrEmpty(errorData) == false)
				throw new InvalidOperationException(errorData);

			foreach (var step in DownloadLicensesAsync(email, password))
				yield return step;
		}
	}
}
