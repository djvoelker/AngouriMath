﻿
/* Copyright (c) 2019-2020 Angourisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
 * modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using AngouriMath.Core;

namespace AngouriMath
{
    partial record Entity
    {
        // Variable, along with Set and Tensor is a unique element, that might be either Continuous or Discrete
        // and/or contain either Continuous or Discrete elements

        #region Variable
        /// <summary>
        /// Variable node. It only has a name.
        /// Construct a <see cref="Variable"/> with an implicit conversion from <see cref="string"/>.
        /// 
        /// </summary>
        public partial record Variable : Entity
        {
            public void Deconstruct(out string name)
                => name = Name;
            internal static Variable CreateVariableUnchecked(string name) => new(name);
            private Variable(string name) => Name = name;
            public string Name { get; }
            public override Priority Priority => Priority.Variable;
            public override Entity Replace(Func<Entity, Entity> func) => func(this);
            protected override Entity[] InitDirectChildren() => Array.Empty<Entity>();
            internal static readonly Variable pi = new Variable(nameof(pi));
            internal static readonly Variable e = new Variable(nameof(e));
            internal static readonly IReadOnlyDictionary<Variable, Number.Complex> ConstantList =
                new Dictionary<Variable, Number.Complex>
                {
                    { pi, MathS.DecimalConst.pi },
                    { e, MathS.DecimalConst.e }
                };
            /// <summary>
            /// Determines whether something is a variable or contant, e. g.
            /// <list type="table">
            ///     <listheader><term>Expression</term> <description>Is it a constant?</description></listheader>
            ///     <item><term>e</term> <description>Yes</description></item>
            ///     <item><term>x</term> <description>No</description></item>
            ///     <item><term>x + 3</term> <description>No</description></item>
            ///     <item><term>pi + 4</term> <description>No</description></item>
            ///     <item><term>pi</term> <description>Yes</description></item>
            /// </list>
            /// </summary>
            public bool IsConstant => ConstantList.ContainsKey(this);
            /// <summary>
            /// Extracts this <see cref="Variable"/>'s name and index
            /// from its <see cref="Name"/> (e. g. "qua" or "phi_3" or "qu_q")
            /// </summary>
            /// <returns>
            /// If this contains _ and valid name and index, returns a pair of
            /// (<see cref="string"/> Prefix, <see cref="string"/> Index),
            /// <see langword="null"/> otherwise
            /// </returns>
            internal (string Prefix, string Index)? SplitIndex() =>
                Name.IndexOf('_') is var pos_ && pos_ == -1
                ? null
                : ((string Prefix, string Index)?)(Name.Substring(0, pos_), Name.Substring(pos_ + 1));
            /// <summary>
            /// Finds next var index name that is unused in <paramref name="expr"/> starting with 1, e. g.
            /// x + n_0 + n_a + n_3 + n_1
            /// will find n_2
            /// </summary>
            /// <remarks>
            /// This is intended for variables visible to the user.
            /// For non-visible variables, use <see cref="CreateTemp"/> instead.
            /// </remarks>
            internal static Variable CreateUnique(Entity expr, string prefix)
            {
                var indices = new HashSet<int>();
                foreach (var var in expr.Vars)
                    if (var.SplitIndex() is var (varPrefix, index)
                        && varPrefix == prefix
                        && int.TryParse(index, out var num))
                        indices.Add(num);
                var i = 1;
                while (indices.Contains(i))
                    i++;
                return new Variable(prefix + "_" + i);
            }
            /// <summary>Creates a temporary variable like %1, %2 and %3 that is not in <paramref name="existingVars"/></summary>
            /// <remarks>
            /// This is intended for variables not visible to the user.
            /// For visible variables, use <see cref="CreateUnique"/> instead.
            /// </remarks>
            internal static Variable CreateTemp(IEnumerable<Variable> existingVars)
            {
                var indices = new HashSet<int>();
                foreach (var var in existingVars)
                    if (var.Name.StartsWith("%") && int.TryParse(var.Name.Substring(1), out var num))
                        indices.Add(num);
                var i = 1;
                while (indices.Contains(i))
                    i++;
                return new Variable("%" + i);
            }
            public static implicit operator Variable(string name) => (Variable)Parser.Parse(name);
        }
        #endregion
    }
}
