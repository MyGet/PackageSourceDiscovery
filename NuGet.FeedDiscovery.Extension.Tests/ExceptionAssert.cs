using System;
using Xunit;

namespace NuGet.Test
{
    public static class ExceptionAssert
    {
        public static void Throws<TException>(Assert.ThrowsDelegate act) where TException : Exception
        {
            Throws<TException>(act, ex => { });
        }

        public static void Throws<TException>(Assert.ThrowsDelegate act, Action<TException> condition) where TException : Exception
        {
            Exception ex = Record.Exception(act);
            Assert.NotNull(ex);
            TException tex = Assert.IsAssignableFrom<TException>(ex);
            condition(tex);
        }

        public static void Throws<TException>(Assert.ThrowsDelegate action, string expectedMessage) where TException : Exception
        {
            Throws<TException>(action, ex => Assert.Equal(expectedMessage, ex.Message));
        }
    }
}