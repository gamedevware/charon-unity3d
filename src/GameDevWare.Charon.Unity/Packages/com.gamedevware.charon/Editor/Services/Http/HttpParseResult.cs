namespace Editor.Services.HttpServer
{
	internal enum HttpParseResult : byte
	{
		Ok = 255,

		MalformedRequestLine = 0,
		UnknownHttpVersion,
		MissingMethod,
		MissingRequestUrl,
		MissingHttpVersion,
		EndOfHeaders,
		MissingHeaderValue,
		MissingHeaderValueEnd,
		ContinuedHeaderValue,
		MalformedHeader
	}
}
