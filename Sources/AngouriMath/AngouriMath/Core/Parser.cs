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

using Antlr4.Runtime;
using System.IO;
using System.Text;
using AngouriMath.Core.NovaSyntax;

[assembly: System.CLSCompliant(false)]
namespace AngouriMath.Core
{
    using Antlr;
    using Exceptions;
    using static ReasonOfFailureWhileParsing;

    /// <summary>
    /// Nesting class for reasons of why parsing could fail. The type union for it
    /// is 
    /// </summary>
    public abstract record ReasonOfFailureWhileParsing
    {
        /// <summary>
        /// The error is an unclassified error returned by the
        /// ANTLR's parser.
        /// </summary>
        public sealed record Unknown(string Reason);


        /// <summary>
        /// This error is when the explicit parsing mode is enabled,
        /// but the input lacks an operator (which is normally inserted
        /// automatically with explicit parsing mode turned off). For
        /// example, "2x" would give this error.
        /// </summary>
        public sealed record MissingOperator(string Details);

        /// <summary>
        /// This is a bug of AngouriMath. Something that we definitely
        /// do not expect. More: <see cref="AngouriMath.Core.Exceptions.AngouriBugException"/>.
        /// Please, report to the main repository.
        /// </summary>
        public sealed record InternalError(string Details);
    }
    
    

    internal static class Parser
    {
        // Antlr parser spams errors into TextWriter provided, we inherit from it to handle lexer/parser errors as ParseExceptions
        private sealed class AngouriMathTextWriter : TextWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
            public override void WriteLine(string? s) => errors.Add(new Unknown(s ?? throw new System.ArgumentNullException()));
            public readonly List<ReasonWhyParsingFailed> errors = new();
        }
        
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
        private static Either<Unit, ReasonWhyParsingFailed> InsertOmittedTokensOrProvideDiagnostic(IList<IToken> tokenList, AngouriMathLexer lexer)
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
        }
        
        internal static Either<Entity, Failure<ReasonWhyParsingFailed>> ParseSilent(string source)
        {
            if (new NovaParser(new NovaLexer(source)).TryParseStatement(out var res))
                return res;
            return new Failure<ReasonWhyParsingFailed>(new Unknown(""));
            /*
            var writer = new AngouriMathTextWriter();

            if (writer.errors.Count > 0)
                return new Failure<ReasonWhyParsingFailed>(writer.errors[0]);

            var lexer = new AngouriMathLexer(new AntlrInputStream(source), null, writer);
            var tokenStream = new CommonTokenStream(lexer);
            tokenStream.Fill();
            var tokenList = tokenStream.GetTokens();            

            if (tokenList.Count is 0)
                return new Failure<ReasonWhyParsingFailed>(
                    new ReasonWhyParsingFailed(
                        new InternalError(
                            $"{nameof(ParseException)} should have been thrown"
                        )
                    )
                );
            
            if (InsertOmittedTokensOrProvideDiagnostic(tokenList, lexer).Is<ReasonWhyParsingFailed>(out var whyFailed))
                return new Failure<ReasonWhyParsingFailed>(whyFailed);

            
            var parser = new AngouriMathParser(tokenStream, null, writer);
            parser.Parse();
            
            if (writer.errors.Count > 0)
                return new Failure<ReasonWhyParsingFailed>(writer.errors[0]);
            
            return parser.Result;*/
        }

        internal static Entity Parse(string source)
            => ParseSilent(source)
                .Switch(
                    valid => valid,
                    failure => failure.Reason.Switch<Entity>(
                            unknown => throw new UnhandledParseException(unknown.Reason),
                            missingOperator => throw new MissingOperatorParseException(missingOperator.Details),
                            internalError => throw new AngouriBugException(internalError.Details)
                    )
                );
    }
}