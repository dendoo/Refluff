using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CarpetCleaning
{
    /// <summary>
    /// Reader for the language files (Resources/Loc/*.json): one flat JSON object of string values, { "key": "text" }.
    /// JsonUtility cannot read a dictionary, and the game should not depend on the Newtonsoft copy an editor package pulls
    /// in, so this is the ~80 lines the format needs. Anything else (nesting, numbers, a duplicate key) is a FormatException.
    /// </summary>
    public static class LocJson
    {
        public static Dictionary<string, string> ParseFlat(string json)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (json == null) throw new FormatException("no text");
            int i = 0;
            if (json.Length > 0 && json[0] == '﻿') i = 1;   // a BOM some editors add
            SkipWs(json, ref i);
            Expect(json, ref i, '{');
            SkipWs(json, ref i);
            if (Peek(json, i) == '}') return result;
            while (true)
            {
                SkipWs(json, ref i);
                string key = ReadString(json, ref i);
                SkipWs(json, ref i);
                Expect(json, ref i, ':');
                SkipWs(json, ref i);
                string value = ReadString(json, ref i);
                if (result.ContainsKey(key)) throw new FormatException("duplicate key \"" + key + "\"");
                result.Add(key, value);
                SkipWs(json, ref i);
                char c = Peek(json, i);
                if (c == ',') { i++; continue; }
                if (c == '}') return result;
                throw new FormatException("expected ',' or '}' at " + i);
            }
        }

        static char Peek(string s, int i) { return i < s.Length ? s[i] : '\0'; }

        static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
        }

        static void Expect(string s, ref int i, char c)
        {
            if (Peek(s, i) != c) throw new FormatException("expected '" + c + "' at " + i);
            i++;
        }

        static string ReadString(string s, ref int i)
        {
            Expect(s, ref i, '"');
            var sb = new StringBuilder();
            while (true)
            {
                if (i >= s.Length) throw new FormatException("unterminated string");
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) throw new FormatException("unterminated escape");
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("short \\u escape at " + i);
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw new FormatException("bad escape \\" + e + " at " + i);
                }
            }
        }
    }
}
