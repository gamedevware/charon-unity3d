using System;
using System.Net;
using System.Net.Security;

namespace GameDevWare.Charon.Unity.Utils
{
	internal sealed class NoCertificateValidationContext : IDisposable
	{
		private readonly RemoteCertificateValidationCallback oldValidationCallback;

		public NoCertificateValidationContext()
		{
			this.oldValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
			ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;

		}

		/// <inheritdoc />
		public void Dispose()
		{
			ServicePointManager.ServerCertificateValidationCallback = this.oldValidationCallback;
		}
	}
}
