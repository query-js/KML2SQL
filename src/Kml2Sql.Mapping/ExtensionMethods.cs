namespace Kml2Sql.Mapping
{
    internal static class ExtensionMethods
    {
        internal static string Sanitize(this string myString)
        {
            return myString.Replace("--", "").Replace(";", "").Replace("'", "\"");
        }
    }
}