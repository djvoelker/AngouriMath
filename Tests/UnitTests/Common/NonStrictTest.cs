﻿using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Functions;
using Xunit;

namespace UnitTests.Common
{
    public class NonStrictTest
    {
        static readonly Entity.Variable x = nameof(x);
        [Fact]
        public void TensorLatex()
        {
            var tens = MathS.Matrices.Matrix(2, 2, 1342, 2123, 1423, 1122);
            Assert.True(tens.Latexise().Length > 16);
        }

        [Fact]
        public void TensorFull()
        {
            var tens = new Entity.Tensor(indices => indices[0] * indices[1] * indices[2], 3, 4, 5);
            Assert.True(tens.ToString().Length > 16);
        }

        [Fact]
        public void EqSysLatex()
        {
            var eq = MathS.Equations(
                "x + 3",
                "y + x + 5"
            );
            Assert.True(eq.Latexise().Length > 10);
        }

        [Fact]
        public void SympySyntax()
        {
            Entity expr = "x + 4 + e";
            Assert.True(MathS.Utils.ToSympyCode(expr).Length > 10);
        }

        [Fact]
        public void TryPoly1()
        {
            Entity expr = "x + x2";
            if (Utils.TryPolynomial(expr, x, out var dst))
                Assert.Equal(MathS.FromString("x2 + x"), dst);
            else
                throw new Xunit.Sdk.XunitException($"{expr} is polynomial");
        }

        [Fact]
        public void TryPoly2()
        {
            Entity expr = "x * (x + x2)";
            if (Utils.TryPolynomial(expr, x, out var dst))
                Assert.Equal(MathS.FromString("x3 + x2"), dst);
            else
                throw new Xunit.Sdk.XunitException($"{expr} is polynomial");
        }

        [Fact]
        public void TryPoly3()
        {
            Entity expr = "x * (x + x2 + z) + y * x";
            if (Utils.TryPolynomial(expr, x, out var dst))
                Assert.Equal(MathS.FromString("x3 + x2 + (y + z) * x"), dst);
            else
                throw new Xunit.Sdk.XunitException($"{expr} is polynomial");
        }
    }
}
