using System;

namespace Smidge
{
    public interface IMinifier
    {
        string Minify(string input);
    }
}