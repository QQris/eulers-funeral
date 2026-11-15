using System;
using EulersFuneral.Core.MathParser;
using NUnit.Framework;

namespace EulersFuneral.Tests.EditMode
{
    public class MathTokenizerAndParserTests
    {
        [Test]
        public void ImplicitMultiplication_EvaluatesCorrectly()
        {
            var fn1 = new MathSurfaceFunction("2x + 3y");
            Assert.AreEqual(13.0, fn1.Evaluate(2.0, 3.0), 1e-9);

            var fn2 = new MathSurfaceFunction("3sen(x)");
            Assert.AreEqual(3.0 * Math.Sin(0.5), fn2.Evaluate(0.5, 0.0), 1e-9);

            var fn3 = new MathSurfaceFunction("(x + 1)(y - 1)");
            Assert.AreEqual(12.0, fn3.Evaluate(3.0, 4.0), 1e-9);

            var fn4 = new MathSurfaceFunction("2pi x");
            Assert.AreEqual(4.0 * Math.PI, fn4.Evaluate(2.0, 0.0), 1e-9);
        }

        [Test]
        public void SpanishFunctionsAndConstants_EvaluateCorrectly()
        {
            var fn = new MathSurfaceFunction("sen(pi/2) + ln(e) + sqrt(16) + abs(-3) + floor(2.7) + ceil(2.1)");
            // 1 + 1 + 4 + 3 + 2 + 3 = 14
            Assert.AreEqual(14.0, fn.Evaluate(0.0, 0.0), 1e-9);
        }

        [Test]
        public void ExponentialDerivative_MatchesItselfDirectly()
        {
            var fn = new MathSurfaceFunction("e^x");
            foreach (double x in new[] { -2.0, -0.5, 0.0, 1.0, 2.5 })
            {
                SurfaceDerivatives d = fn.EvaluateWithDerivatives(x, 0.0);
                double expected = Math.Exp(x);
                Assert.IsTrue(d.IsDefined);
                Assert.AreEqual(expected, d.Z, 1e-9);
                Assert.AreEqual(expected, d.Fx, 1e-9);
                Assert.AreEqual(expected, d.Fxx, 1e-9);
                Assert.AreEqual(0.0, d.Fy, 1e-9);
            }
        }

        [Test]
        public void UndefinedPoints_ReturnNaN()
        {
            var divZero = new MathSurfaceFunction("1 / x");
            Assert.IsTrue(double.IsNaN(divZero.Evaluate(0.0, 1.0)));

            var negSqrt = new MathSurfaceFunction("sqrt(x)");
            Assert.IsTrue(double.IsNaN(negSqrt.Evaluate(-4.0, 0.0)));

            var nonPosLn = new MathSurfaceFunction("ln(x)");
            Assert.IsTrue(double.IsNaN(nonPosLn.Evaluate(0.0, 0.0)));
            Assert.IsTrue(double.IsNaN(nonPosLn.Evaluate(-2.0, 0.0)));
        }

        [Test]
        public void InvalidSyntax_ProducesSpanishErrorMessage()
        {
            bool ok1 = MathParser.TryParse("sin(x)", out _, out string err1);
            Assert.IsFalse(ok1);
            StringAssert.Contains("sen", err1);

            bool ok2 = MathParser.TryParse("2x + (3y", out _, out string err2);
            Assert.IsFalse(ok2);
            StringAssert.Contains("paréntesis", err2);
        }
    }
}
