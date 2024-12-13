namespace LethalMin
{
public static class StringExtensions
{
    public static string RemoveAfterLastSlash(this string str)
    {
        int lastIndex = str.LastIndexOf('/');
        return lastIndex != -1  str.Substring(0, lastIndex + 1) : str;
    }
}
}