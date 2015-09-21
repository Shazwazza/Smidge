using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Smidge.FileProcessors
{
    ///////////////////////////////////////////////////////////////////////
    //                             CssMinifier                           //
    //             Written by: Miron Abramson. Date: 24-06-08            //
    //                    Last updated: 26-06-08                        //
    ///////////////////////////////////////////////////////////////////////
    /*
        Based on the code of jsmin by Douglas Crockford  (www.crockford.com)
        Modified for CSS prupose by: Miron Abramson
        Modified again for Smidge which fixed lots of whitespace issues
    */

    public sealed class CssMinifier : IPreProcessor
    {
        /// <summary>
        /// Minifies Css
        /// </summary>
        /// <param name="fileProcessContext"></param>
        /// <returns></returns>
        public Task<string> ProcessAsync(FileProcessContext fileProcessContext)
        {
            using (var reader = new StringReader(fileProcessContext.FileContent))
            {
                return Task.FromResult(Minify(reader));
            }
        }

        const int EOF = -1;

        TextReader tr;
        StringBuilder sb;
        int theA;
        int theB;
        int theLookahead = EOF;


        /// <summary>
        /// Minify the input script
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public string Minify(TextReader reader)
        {
            sb = new StringBuilder();
            tr = reader;
            theA = '\n';
            theB = 0;
            theLookahead = EOF;
            cssmin();
            return sb.ToString();
        }

        /// <summary>
        /// Excute the actual minify
        /// </summary>
        void cssmin()
        {
            action(3);
            while (theA != EOF)
            {
                switch (theA)
                {
                    case ' ':
                        {
                            switch (theB)
                            {
                                case ' ':        //body.Replace("  ", String.Empty);
                                case '{':        //body = body.Replace(" {", "{");
                                case ':':        //body = body.Replace(" {", "{");
                                case '\n':       //body = body.Replace(" \n", "\n");
                                case '\r':       //body = body.Replace(" \r", "\r");
                                case '\t':       //body = body.Replace(" \t", "\t");
                                    action(2);
                                    break;
                                default:
                                    action(1);
                                    break;
                            }
                            break;
                        }
                    case '\t':              //body = body.Replace("\t", "");
                    case '\r':              //body = body.Replace("\r", "");
                        action(2);
                        break;
                    case '\n':              //body = body.Replace("\n", "");
                        if (char.IsWhiteSpace((char)theB))
                        {
                            //skip over whitespace
                            action(3);
                        }
                        else
                        {
                            //convert the line break to a space except when in the beginning
                            //TODO: this isn't the best place to put this logic since all puts are done
                            // in the action, but i don't see any other way to do this,
                            //we could set theA = ' ' and call action(1) ?
                            if (sb.Length > 0) put(' ');
                            action(2);
                        }
                        break;
                    case '}':
                    case '{':
                    case ':':
                    case ',':
                    case ';':
                        //skip over whitespace
                        action(char.IsWhiteSpace((char)theB) ? 3 : 1);
                        break;
                    default:
                        action(1);
                        break;
                }
            }
        }
        /* action -- do something! What you do is determined by the argument:
                1   Output A. Copy B to A. Get the next B.
                2   Copy B to A. Get the next B. (Delete A).
                3   Get the next B. (Delete B).
        */
        void action(int d)
        {
            if (d <= 1)
            {
                put(theA);
            }
            if (d <= 2)
            {
                theA = theB;
                if (theA == '\'' || theA == '"')
                {
                    for (;;)
                    {
                        put(theA);
                        theA = get();
                        if (theA == theB)
                        {
                            break;
                        }
                        if (theA <= '\n')
                        {
                            throw new FormatException(string.Format("Error: unterminated string literal: {0}\n", theA));
                        }
                        if (theA == '\\')
                        {
                            put(theA);
                            theA = get();
                        }
                    }
                }
            }
            if (d <= 3)
            {
                theB = next();
                if (theB == '/' && (theA == '(' || theA == ',' || theA == '=' ||
                                    theA == '[' || theA == '!' || theA == ':' ||
                                    theA == '&' || theA == '|' || theA == '?' ||
                                    theA == '{' || theA == '}' || theA == ';' ||
                                    theA == '\n'))
                {
                    put(theA);
                    put(theB);
                    for (;;)
                    {
                        theA = get();
                        if (theA == '/')
                        {
                            break;
                        }
                        else if (theA == '\\')
                        {
                            put(theA);
                            theA = get();
                        }
                        else if (theA <= '\n')
                        {
                            throw new FormatException(string.Format("Error: unterminated Regular Expression literal : {0}.\n", theA));
                        }
                        put(theA);
                    }
                    theB = next();
                }
            }
        }
        /* next -- get the next character, excluding comments. peek() is used to see
                if a '/' is followed by a '*'.
        */
        int next()
        {
            int c = get();
            if (c == '/')
            {
                switch (peek())
                {
                    case '*':
                        {
                            get();
                            for (;;)
                            {
                                switch (get())
                                {
                                    case '*':
                                        {
                                            if (peek() == '/')
                                            {
                                                get();
                                                return ' ';
                                            }
                                            break;
                                        }
                                    case EOF:
                                        {
                                            throw new FormatException("Error: Unterminated comment.\n");
                                        }
                                }
                            }
                        }
                    default:
                        {
                            return c;
                        }
                }
            }
            return c;
        }
        /* peek -- get the next character without getting it.
        */
        int peek()
        {
            theLookahead = get();
            return theLookahead;
        }
        /* get -- return the next character from stdin. Watch out for lookahead. If
                the character is a control character, translate it to a space or
                linefeed.
        */
        int get()
        {
            int c = theLookahead;
            theLookahead = EOF;
            if (c == EOF)
            {
                c = tr.Read();
            }
            if (c >= ' ' || c == '\n' || c == EOF)
            {
                return c;
            }
            if (c == '\r')
            {
                return '\n';
            }
            return ' ';
        }
        void put(int c)
        {
            sb.Append((char)c);
        }
    }

    ///// <summary>
    ///// Simple css minifier
    ///// </summary>
    //public sealed class CssMinifier : IPreProcessor
    //{
    //    /// <summary>
    //    /// Minifies Css
    //    /// </summary>
    //    /// <param name="input"></param>
    //    /// <returns></returns>
    //    public Task<string> ProcessAsync(FileProcessContext fileProcessContext)
    //    {
    //        var input = fileProcessContext.FileContent;
    //        input = Regex.Replace(input, @"[\n\r]+\s*", string.Empty);
    //        input = Regex.Replace(input, @"\s+", " ");
    //        input = Regex.Replace(input, @"\s?([:,;{}])\s?", "$1");
    //        input = Regex.Replace(input, @"([\s:]0)(px|pt|%|em)", "$1");
    //        input = Regex.Replace(input, @"/\*[\d\D]*?\*/", string.Empty);
    //        return Task.FromResult(input);
    //    }
    //}
}