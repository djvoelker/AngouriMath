//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

/*

The parser source files under the Antlr folder other than "Angourimath.g" are generated by ANTLR.
You should only modify "Angourimath.g", other source files are generated from this file.
Any modifications to other source files will be overwritten when the parser is regenerated.

*/

using System.IO;
using System.Text;
using AngouriMath.Core.NovaSyntax;
using Yoakke.Streams;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Text;
using Nova = AngouriMath.Core.NovaSyntax.AngouriMathTokenType;

[assembly: System.CLSCompliant(false)]
namespace AngouriMath.Core
{
    using Exceptions;

    internal static class Parser
    {
        
        // TODO: how to sync it with the lexer?
        [ConstantField]
        private static readonly HashSet<string> keywords = new (new []
        {
            "apply","lambda","integral","derivative","gamma","limit","limitleft","limitright","signum","sgn","sign","abs","phi","domain","piecewise","log","sqrt","cbrt","sqr","ln","sin","cos","tan","cot","cotan","sec","cosec","csc","arcsin","arccos","arctan","arccotan","arcsec","arccosec","arccsc","acsc","asin","acos","atan","acotan","asec","acosec","acot","arccot","sinh","sh","cosh","ch","tanh","th","cotanh","coth","cth","sech","sch","cosech","csch","asinh","arsinh","arsh","acosh","arcosh","arch","atanh","artanh","arth","acoth","arcoth","acotanh","arcotanh","arcth","asech","arsech","arsch","acosech","arcosech","arcsch","acsch"
        });

        private static List<Token<AngouriMathTokenType>> InsertOmittedOperators(IReadOnlyList<Token<AngouriMathTokenType>> tokens)
        {
            var list = new List<Token<AngouriMathTokenType>>();
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var (left, right) = (tokens[i], tokens[i + 1]);
                list.Add(left);
                Token<AngouriMathTokenType>? toInsert = (left, right) switch
                {
                    // 2x -> 2 * x       2sqrt -> 2 * sqrt       2( -> 2 * (
                    // )x -> ) * x       )sqrt -> ) * sqrt       )( -> ) * (
                    ( { Kind: Nova.Number } or { Kind: Nova.Punctuation, Text: ")" }, { Kind: Nova.Identifier or Nova.Keyword } or { Kind: Nova.Punctuation, Text: "(" } )
                        => new(default, "*", Nova.Operator),
                    
                    // x y -> x * y      x sqrt -> x * sqrt      x( -> x * (
                    ( { Kind: Nova.Identifier, Text: var varName }, { Kind: Nova.Identifier or Nova.Keyword } or { Kind: Nova.Punctuation, Text: "(" } )
                        when !keywords.Contains(varName) => new(default, "*", Nova.Operator),
                    
                    // 3 2 -> 3 ^ 2      )2 -> ) ^ 2
                    ( { Kind: Nova.Number } or { Kind: Nova.Punctuation, Text: ")" }, { Kind: Nova.Number } )
                        => new(default, "^", Nova.Operator),
                    
                    // x2 -> x ^ 2
                    ( { Kind: Nova.Identifier, Text: var varName }, { Kind: Nova.Number } )
                        when !keywords.Contains(varName) => new(default, "^", Nova.Operator),
                    
                    _ => null
                };
                if (toInsert is { } actualToken)
                    list.Add(actualToken);
            }
            list.Add(tokens[^1]);
            return list;
        }
        
        internal static Either<Entity, Failure<string>, ParseException> ParseSilent(string source)
        {
            var lexer = new NovaLexer(source);
            var tokens = lexer.LexAll().ToList();            

            if (!tokens.Any())
                return new Failure<string>("Empty expression");
            
            if (!MathS.Settings.ExplicitParsingOnly)
                tokens = InsertOmittedOperators(tokens);
            
            try
            {
                var result = new NovaParser(tokens).ParseStatement();

                if (result.IsError)
                {
                    var err = result.Error;
                    var sb = new StringBuilder();
                    foreach (var element in err.Elements.Values)
                    {
                        sb
                            .Append($"Expected ")
                            .Append(string.Join(" or ", (IEnumerable<object>)element.Expected))
                            .Append(" while parsing ")
                            .Append(element.Context)
                            .Append("\n");
                    }
                    sb.Append($"But got {(err.Got == null ? "end of input" : err.Got)}");
                    return new Failure<string>(sb.ToString());
                }
                return result.Ok.Value;
            } catch (ParseException pe)
            {
                return pe;
            }
        }

        internal static Entity Parse(string source)
            => ParseSilent(source)
                .Switch(
                    valid => valid,
                    failure => throw new UnhandledParseException(failure.Reason),
                    exception => throw exception
                    );
    }
}