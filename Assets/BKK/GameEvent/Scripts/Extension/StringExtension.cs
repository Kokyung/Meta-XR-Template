using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BKK.Extension
{
    public static class StringUtility
    {
        static readonly StringBuilder k_StringBuilder = new StringBuilder();
        
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static bool ContainsWords(this string source, string toCheck, StringComparison comp)
        {
            string[] words = toCheck.Split();

            foreach (var word in words)
            {
                if (!source.Contains(word, comp)) return false;
            }

            return true;
        }

        public static string EraseWhiteSpace(this string source)
        {
            return string.Concat(source.Where(c => !char.IsWhiteSpace(c)));
        }

        public static string GetPath(this Component component) 
        {
            return component.transform.GetPath() + "/" + component.GetType().ToString();
        }
    
        public static string GetPath(this Transform current) 
        {
            if (current.parent == null) return current.name;
            return current.parent.GetPath() + "/" + current.name;
        }

        public static void CopyToClipBoard(string str)
        {
            GUIUtility.systemCopyBuffer = str;
        }
        
        public static string[] GetStringArrayFromEnum(this Type t)
        {
            return Enum.GetNames(t);
        }
        
        public static int GetEnumCount(this Type t)
        {
            return Enum.GetNames(t).Length;
        }
        
        public static T ConvertToEnum<T>(this string _str)
        {
            try { return (T)Enum.Parse(typeof(T), _str); }
            catch { return (T)Enum.Parse(typeof(T), "none"); }
        }
        
        public static string ConvertToString<T>(this T _enum) where T : Enum
        {
            try { return Enum.GetName(typeof(T), _enum); }
            catch { return String.Empty; }
        }

        public static string GetUniqueName(this string baseName, List<string> names)
        {
            HashSet<string> nameSet = new HashSet<string>(names);

            if (!nameSet.Contains(baseName)) return baseName;

            int suffix = 1;
            string candidate;

            do
            {
                candidate = $"{baseName} ({suffix})";
                suffix++;
            } while (nameSet.Contains(candidate));

            return candidate;
        }
        
        public static string InsertSpacesBetweenWords(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            k_StringBuilder.Length = 0;
            k_StringBuilder.Append(str[0]);

            var strLength = str.Length;
            for (var i = 0; i < strLength - 1; i++)
            {
                var thisChar = str[i];
                var nextChar = str[i + 1];

                var firstIsLower = char.IsLower(thisChar);
                var secondIsLower = char.IsLower(nextChar);

                // Need a space when lower case followed by upper case eg. aB -> a B
                var needsSpace = firstIsLower && !secondIsLower;

                if (i + 2 < strLength)
                {
                    // Also need space at the beginning of a word after an all-uppercase word eg. ABc -> A Bc
                    var nextNextChar = str[i + 2];
                    var thirdIsLower = char.IsLower(nextNextChar);
                    needsSpace |= !firstIsLower && !secondIsLower && thirdIsLower;
                }

                if (needsSpace)
                    k_StringBuilder.Append(' ');

                k_StringBuilder.Append(nextChar);
            }

            return k_StringBuilder.ToString();
        }
        
        public static string ToUpperFirstChar(this string input)
        {
            string newString = input;
            
            if (!String.IsNullOrEmpty(newString) && Char.IsLower(newString[0]))
            {
                newString = Char.ToUpper(newString[0]) + newString.Substring(1);
            }
            
            return newString;
        }
        
        public static string ToLowerFirstChar(this string input)
        {
            string newString = input;
            
            if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
            {
                newString = Char.ToLower(newString[0]) + newString.Substring(1);
            }
            
            return newString;
        }
    }
}
