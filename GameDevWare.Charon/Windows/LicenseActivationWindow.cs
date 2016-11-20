/*
	Copyright (c) 2016 Denis Zykov

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
using System.IO;
using GameDevWare.Charon.Models;
using GameDevWare.Charon.Tasks;
using GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

// ReSharper disable UnusedMember.Local

namespace GameDevWare.Charon.Windows
{
	internal class LicenseActivationWindow : EditorWindow
	{
		private enum Mode { SignIn, SelectLicense, SignUp }
		private string email;
		private string password;
		private string passwordRetype;
		private string firstName;
		private string lastName;
		private string organizationName = "Private Organization";
		private string unityInvoiceNumber;
		private string errorMessage;
		private string selectedLicense;
		private Mode mode = Mode.SignIn;
		[NonSerialized]
		private LicenseInfo[] licenses;
		[NonSerialized]
		private bool[] licensesFoldouts;
		[NonSerialized]
		private Promise currentProcess = Promise.Fulfilled;

		private event EventHandler Done;
		private event EventHandler<ErrorEventArgs> Cancel;

		public LicenseActivationWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWLICENSEACTIVATIONTITLE);
			this.maxSize = minSize = new Vector2(400, 400);
			this.position = new Rect(
				(Screen.width - this.maxSize.x) / 2,
				(Screen.height - this.maxSize.y) / 2,
				this.maxSize.x,
				this.maxSize.y
			);
		}

		public static Promise ShowAsync(bool autoClose = true)
		{
			var promise = new Promise();
			var window = GetWindow<LicenseActivationWindow>(utility: true);

			window.Done += (sender, args) => promise.TrySetCompleted();
			window.Cancel += (sender, args) => promise.TrySetFailed(args.GetException());

			window.Focus();

			return promise;
		}

		protected void OnGUI()
		{
			switch (mode)
			{
				case Mode.SignIn:
					if (string.IsNullOrEmpty(errorMessage) == false)
						EditorGUILayout.HelpBox(this.errorMessage, MessageType.Error);
					GUILayout.Space(18);
					this.email = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWEMAIL, this.email);
					this.password = EditorGUILayout.PasswordField(Resources.UI_UNITYPLUGIN_WINDOWPASSWORD, this.password);
					GUILayout.Space(18);
					GUILayout.BeginHorizontal();
					EditorGUILayout.Space();
					GUI.enabled = this.currentProcess.IsCompleted;
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWREGISTERBUTTON, GUILayout.Width(80)))
					{
						this.mode = Mode.SignUp;
						this.Repaint();
					}
					GUI.enabled = this.currentProcess.IsCompleted && !string.IsNullOrEmpty(this.email) && !string.IsNullOrEmpty(this.password);
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWSIGNINBUTTON, GUILayout.Width(80)) || (GUI.enabled && Event.current.isKey && Event.current.keyCode == KeyCode.Return))
					{
						this.SignIn();
					}
					GUI.enabled = true;
					GUILayout.EndHorizontal();
					break;
				case Mode.SelectLicense:
					GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOWSELECTLICENSELABEL, EditorStyles.boldLabel);
					if (string.IsNullOrEmpty(errorMessage) == false)
						EditorGUILayout.HelpBox(this.errorMessage, MessageType.Error);
					if (this.licenses == null)
					{
						if (Event.current.type == EventType.repaint)
							this.mode = Mode.SignIn;
						return;
					}
					GUILayout.Space(18);

					for (var i = 0; i < this.licenses.Length; i++)
					{
						var license = this.licenses[i];
						GUILayout.BeginHorizontal();
						if (GUILayout.Toggle(string.Equals(this.selectedLicense, license.SerialNumber), "", GUILayout.Width(13)))
							this.selectedLicense = license.SerialNumber;
						this.licensesFoldouts[i] = EditorGUILayout.Foldout(this.licensesFoldouts[i], license.Recipient.FirstName + " " + license.Recipient.LastName + " (" + license.Organization.Name + ")");
						GUILayout.EndHorizontal();
						if (this.licensesFoldouts[i])
						{
							EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOWTYPE, license.Type);
							EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOWSERIALNUMBER, license.SerialNumber);
							EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOWRECIPIENTID, license.Recipient.Id);
							EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOWORGANIZATIONID, license.Organization.Id);
							if (license.ExpirationDate - DateTime.UtcNow < TimeSpan.FromDays(1000))
								EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOWEXPIRATION, license.ExpirationDate.ToShortDateString());
						}
					}

					GUILayout.Space(18);
					GUILayout.BeginHorizontal();
					EditorGUILayout.Space();
					GUI.enabled = string.IsNullOrEmpty(this.selectedLicense) == false;
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWOKBUTTON, GUILayout.Width(80)))
					{
						Settings.Current.SelectedLicense = this.selectedLicense;
						Settings.Current.Save();
						this.RaiseDone();
					}
					GUI.enabled = true;
					GUILayout.EndHorizontal();
					break;
				case Mode.SignUp:
					GUI.enabled = this.currentProcess.IsCompleted;
					if (string.IsNullOrEmpty(errorMessage) == false)
						EditorGUILayout.HelpBox(this.errorMessage, MessageType.Error);
					GUILayout.Space(18);
					this.email = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWEMAIL, this.email);
					this.password = EditorGUILayout.PasswordField(Resources.UI_UNITYPLUGIN_WINDOWPASSWORD, this.password);
					this.passwordRetype = EditorGUILayout.PasswordField(Resources.UI_UNITYPLUGIN_WINDOWRETYPEPASSWORD, this.passwordRetype);
					this.firstName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWFIRSTNAME, this.firstName);
					this.lastName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWLASTNAME, this.lastName);
					this.organizationName = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWORGANIZATION, this.organizationName);
					this.unityInvoiceNumber = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWASSETSTOREINVOICENUMBER, this.unityInvoiceNumber);

					GUILayout.Space(18);
					GUILayout.BeginHorizontal();
					EditorGUILayout.Space();
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWBACKBUTTON, GUILayout.Width(80)))
					{
						this.mode = Mode.SignIn;
						this.Repaint();
					}
					GUI.enabled = string.IsNullOrEmpty(this.email) == false &&
								  string.IsNullOrEmpty(this.password) == false &&
								  string.IsNullOrEmpty(this.passwordRetype) == false &&
								  string.IsNullOrEmpty(this.firstName) == false &&
								  string.IsNullOrEmpty(this.lastName) == false &&
								  string.IsNullOrEmpty(this.organizationName) == false &&
								  string.IsNullOrEmpty(this.unityInvoiceNumber) == false &&
								  this.currentProcess.IsCompleted;

					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWOKBUTTON, GUILayout.Width(80)) || (GUI.enabled && Event.current.isKey && Event.current.keyCode == KeyCode.Return))
					{
						this.SignUp();
					}
					GUI.enabled = true;
					GUILayout.EndHorizontal();
					break;
			}

			GUILayoutUtility.GetRect(1, 1, 1, 1);
			if (Event.current.type == EventType.repaint && GUILayoutUtility.GetLastRect().y > 0)
			{
				var newRect = GUILayoutUtility.GetLastRect();
				this.position = new Rect(this.position.position, new Vector2(this.position.width, newRect.y + 7));
				this.minSize = this.maxSize = this.position.size;
			}
		}

		private void SignIn()
		{
			if (string.IsNullOrEmpty(this.email) || string.IsNullOrEmpty(this.password))
				return;

			this.errorMessage = null;
			this.licenses = null;

			var getLicenses = Licenses.DownloadLicenses(this.email, this.password, scheduleCoroutine: true);
			this.currentProcess = getLicenses;
			getLicenses.ContinueWith(this.OnLicensesAcquired);
			this.Repaint();
		}
		private void SignUp()
		{
			if (string.IsNullOrEmpty(this.email) ||
				string.IsNullOrEmpty(this.password) ||
				string.IsNullOrEmpty(this.passwordRetype) ||
				string.IsNullOrEmpty(this.firstName) ||
				string.IsNullOrEmpty(this.lastName) ||
				string.IsNullOrEmpty(this.organizationName) ||
				string.IsNullOrEmpty(this.unityInvoiceNumber))
			{
				return;
			}

			this.errorMessage = null;
			this.licenses = null;

			if (this.password != this.passwordRetype)
			{
				this.errorMessage = Resources.UI_UNITYPLUGIN_WINDOWPASSWORDSDOESNMATCHMESSAGE;
				this.Repaint();
				return;
			}

			var register = Licenses.Register(this.firstName, this.lastName, this.organizationName, this.email, this.password, this.unityInvoiceNumber, scheduleCoroutine: true);
			this.currentProcess = register;
			register.ContinueWith(this.OnLicensesAcquired);
			this.Repaint();
		}
		private void OnLicensesAcquired(Promise<LicenseInfo[]> getLicensePromise)
		{
			if (getLicensePromise.HasErrors)
			{
				this.errorMessage = getLicensePromise.Error.Unwrap().Message;
				this.Repaint();
				return;
			}

			this.licenses = getLicensePromise.GetResult();
			if (this.licenses.Length == 0)
			{
				this.errorMessage = Resources.UI_UNITYPLUGIN_WINDOWNOAVAILABLELICENSESMESSAGE;
				this.Repaint();
			}
			else if (licenses.Length > 1)
			{
				this.selectedLicense = Settings.Current.SelectedLicense;
				this.licenses = getLicensePromise.GetResult();
				this.licensesFoldouts = new bool[this.licenses.Length];
				this.mode = Mode.SelectLicense;
				this.Repaint();
			}
			else
			{
				Settings.Current.SelectedLicense = this.selectedLicense = this.licenses[0].SerialNumber;
				Settings.Current.Save();
				this.RaiseDone();
			}
		}

		private void RaiseDone()
		{
			if (this.Done != null)
				this.Done(this, EventArgs.Empty);
			this.Done = null;
			this.Cancel = null;

			this.Close();
		}
		private void RaiseCancel()
		{
			if (this.Cancel != null)
			{
				this.Cancel(this, new ErrorEventArgs(new InvalidOperationException(Resources.UI_UNITYPLUGIN_OPERATIONCANCELLED)));
				if (Settings.Current.Verbose)
					Debug.Log(string.Format("'{0}' window is closed by user.", this.titleContent.text));
			}
			this.Cancel = null;
			this.Done = null;
		}

		private void OnDestroy()
		{
			this.RaiseCancel();
		}
	}
}
