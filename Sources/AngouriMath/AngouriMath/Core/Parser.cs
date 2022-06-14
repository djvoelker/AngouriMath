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
using IToken = Antlr4.Runtime.IToken;
using Nova = AngouriMath.Core.NovaSyntax.AngouriMathTokenType;

[assembly: System.CLSCompliant(false)]
namespace AngouriMath.Core
{
    using Antlr;
    using Exceptions;

    internal static class Parser
    {
        
        private static int? GetNextToken(IList<IToken> tokens, int currPos)
        {
            while (tokens[currPos].Channel != 0)
                if (++currPos >= tokens.Count)
                    return null;
            return currPos;
        }
        
        /// <summary>
        /// This method inserts omitted tokens when no
        /// explicit-only parsing is enabled. Otherwise,
        /// it will throw an exception.
        /// </summary>
        /*
        private static Either<Unit, string> InsertOmittedTokensOrProvideDiagnostic(IList<IToken> tokenList, AngouriMathLexer lexer)
        {
            const string NUMBER = nameof(NUMBER);
            const string VARIABLE = nameof(VARIABLE);
            const string PARENTHESIS_OPEN = "'('";
            const string PARENTHESIS_CLOSE = "')'";
            const string FUNCTION_OPEN = "\x1"; // Fake display name for all function tokens e.g. "'sin('"
         
            if (GetNextToken(tokenList, 0) is not { } leftId)
                return new Unit();
            
            for (var rightId = leftId + 1; rightId < tokenList.Count; leftId = rightId++)
            {
                if (GetNextToken(tokenList, rightId) is not { } nextRightId)
                    return new Unit();
                rightId = nextRightId;
                if ((GetType(tokenList[leftId]), GetType(tokenList[rightId])) switch
                    {
                        // 2x -> 2 * x       2sqrt -> 2 * sqrt       2( -> 2 * (
                        // x y -> x * y      x sqrt -> x * sqrt      x( -> x * (
                        // )x -> ) * x       )sqrt -> ) * sqrt       )( -> ) * (
                        (NUMBER or VARIABLE or PARENTHESIS_CLOSE, VARIABLE or FUNCTION_OPEN or PARENTHESIS_OPEN)
                            => lexer.Multiply,
                        // 3 2 -> 3 ^ 2      x2 -> x ^ 2             )2 -> ) ^ 2
                        (NUMBER or VARIABLE or PARENTHESIS_CLOSE, NUMBER) => lexer.Power,

                        _ => null
                    } is { } insertToken)
                    {
                        if (!MathS.Settings.ExplicitParsingOnly)
                            // Insert at j because we need to keep the first one behind
                            tokenList.Insert(rightId, insertToken);
                        else
                            return new ReasonWhyParsingFailed(new MissingOperator($"There should be an operator between {tokenList[leftId]} and {tokenList[rightId]}"));
                    }
            }
            
            return new Unit();
            
            static string GetType(IToken token) =>
                AngouriMathLexer.DefaultVocabulary.GetDisplayName(token.Type) is var type
                && type is not PARENTHESIS_OPEN && type.EndsWith("('") ? FUNCTION_OPEN : type;
        }*/
        
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
                    ( { Kind: Nova.Number } or { Kind: Nova.Punctuation, Text: ")" }, { Kind: Nova.Identifier } or { Kind: Nova.Punctuation, Text: "(" } )
                        => new(default, "*", Nova.Operator),
                    
                    // x y -> x * y      x sqrt -> x * sqrt      x( -> x * (
                    ( { Kind: Nova.Identifier, Text: var varName }, { Kind: Nova.Identifier } or { Kind: Nova.Punctuation, Text: "(" } )
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
        
        internal static Either<Entity, Failure<string>> ParseSilent(string source)
        {
            var lexer = new NovaLexer(source);
            var tokens = lexer.LexAll().ToList();            

            if (!tokens.Any())
                return new Failure<string>("Empty expression");
            
            if (!MathS.Settings.ExplicitParsingOnly)
                tokens = InsertOmittedOperators(tokens);

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
        }

        internal static Entity Parse(string source)
            => ParseSilent(source)
                .Switch(
                    valid => valid,
                    failure => throw new UnhandledParseException(failure.Reason)
                    );
    }
}