using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

// A bounded JSON reader for GitHub's public release metadata.
internal sealed class JsonLite
{
    readonly string source;
    int offset;
    JsonLite(string json){source=json;}
    public static object Parse(string json)
    {
        if(json==null || json.Length>1024*1024)throw new InvalidDataException("Respuesta demasiado grande.");
        var parser=new JsonLite(json);object result=parser.Value(0);parser.White();
        if(parser.offset!=json.Length)throw new InvalidDataException("Respuesta JSON inválida.");
        return result;
    }
    void White(){while(offset<source.Length && char.IsWhiteSpace(source[offset]))offset++;}
    bool Take(char c){White();if(offset<source.Length && source[offset]==c){offset++;return true;}return false;}
    void Need(char c){if(!Take(c))throw new InvalidDataException("Respuesta JSON inválida.");}
    object Value(int depth)
    {
        if(depth>32)throw new InvalidDataException("Respuesta JSON demasiado profunda.");
        White();if(offset==source.Length)throw new InvalidDataException("Respuesta JSON incompleta.");
        if(source[offset]=='"')return String();
        if(Take('{')){
            var dict=new Dictionary<string,object>();
            if(Take('}'))return dict;
            do {string key=String();Need(':');dict[key]=Value(depth+1);if(Take('}'))return dict;Need(',');}while(true);
        }
        if(Take('[')){
            var list=new List<object>();if(Take(']'))return list;
            do {list.Add(Value(depth+1));if(Take(']'))return list;Need(',');}while(true);
        }
        if(source[offset]=='-' || char.IsDigit(source[offset])){
            int start=offset;
            while(offset<source.Length && "-+0123456789.eE".IndexOf(source[offset])>=0)offset++;
            double value;
            if(!double.TryParse(source.Substring(start,offset-start),NumberStyles.Float,CultureInfo.InvariantCulture,out value) || double.IsInfinity(value))
                throw new InvalidDataException("Número JSON inválido.");
            return value;
        }
        foreach(string literal in new[]{"true","false","null"})if(source.Length-offset>=literal.Length &&
            string.CompareOrdinal(source,offset,literal,0,literal.Length)==0){offset+=literal.Length;return literal=="null"?null:(object)(literal=="true");}
        throw new InvalidDataException("Respuesta JSON inválida.");
    }
    string String()
    {
        Need('"');var result=new StringBuilder();
        while(offset<source.Length){
            char c=source[offset++];if(c=='"')return result.ToString();
            if(c<' ')throw new InvalidDataException("Texto JSON inválido.");
            if(c!='\\'){result.Append(c);continue;}
            if(offset>=source.Length)break;
            char escape=source[offset++];
            switch(escape){
                case '"':result.Append('"');break;case '\\':result.Append('\\');break;case '/':result.Append('/');break;
                case 'b':result.Append('\b');break;case 'f':result.Append('\f');break;case 'n':result.Append('\n');break;
                case 'r':result.Append('\r');break;case 't':result.Append('\t');break;
                case 'u':
                    if(offset+4>source.Length)throw new InvalidDataException("Unicode JSON inválido.");
                    int code;if(!int.TryParse(source.Substring(offset,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out code))
                        throw new InvalidDataException("Unicode JSON inválido.");
                    result.Append((char)code);offset+=4;break;
                default:throw new InvalidDataException("Escape JSON inválido.");
            }
        }
        throw new InvalidDataException("Cadena JSON incompleta.");
    }
}
