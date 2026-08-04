using System.Text.Json;
using System.Text.Json.Serialization;

namespace PROCTOR.API.Middleware;

/// <summary>
/// Strips characters PostgreSQL refuses to store in a text column from every inbound string.
///
/// PostgreSQL text/varchar cannot hold a NUL (U+0000) byte — writing one aborts the transaction
/// with `22021: invalid byte sequence for encoding "UTF8": 0x00`. Users routinely paste case
/// descriptions out of Word/PDF, which is exactly where stray NULs and other C0 control codes come
/// from, so the whole case submission failed on content that looked perfectly normal on screen.
///
/// Cleaning at the JSON boundary fixes every endpoint at once rather than field by field.
/// Tab, newline and carriage return are kept — multi-line descriptions need them.
/// </summary>
public class SanitizedStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return Sanitize(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);

    public static string? Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Fast path: the overwhelming majority of payloads contain nothing to strip.
        var needsCleaning = false;
        foreach (var ch in value)
        {
            if (IsDisallowed(ch)) { needsCleaning = true; break; }
        }
        if (!needsCleaning) return value;

        var buffer = new char[value.Length];
        var written = 0;
        foreach (var ch in value)
            if (!IsDisallowed(ch)) buffer[written++] = ch;

        return new string(buffer, 0, written);
    }

    // C0 control codes (except the whitespace ones we want to keep) and DEL.
    private static bool IsDisallowed(char ch) =>
        (ch < ' ' && ch != '\t' && ch != '\n' && ch != '\r') || ch == '\u007F';
}
