namespace ProtoKey.Storage;

public static class KeyValidator
{
    public static bool IsValidKey(string? key) =>
        key is { Length: >= 1 and <= 1000 } && HasOnlyAllowedCharacters(key);

    public static bool IsValidPrefix(string prefix) =>
        prefix.Length <= 1000 && HasOnlyAllowedCharacters(prefix);

    private static bool HasOnlyAllowedCharacters(string value)
    {
        foreach (char symbol in value)
        {
            bool allowed =
                symbol is >= 'a' and <= 'z' or
                >= 'A' and <= 'Z' or
                >= '0' and <= '9' or
                '_' or '-' or '.';
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }
}
