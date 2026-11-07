using NUnit.Framework;
using Strawhenge.Common.Logging;
using System;
using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Writes every message to the test output and records it, so tests can assert on what was logged.
    // Errors and exceptions fail the test, as Unity's test runner does for an unexpected Debug.LogError.
    class FakeLogger : ILogger
    {
        readonly List<string> _messages = new();
        readonly List<string> _warnings = new();

        // Everything logged, at any level.
        public IReadOnlyList<string> Messages => _messages;

        public IReadOnlyList<string> Warnings => _warnings;

        public void LogInformation(string message)
        {
            Record("Information", message);
        }

        public void LogWarning(string message)
        {
            Record("Warning", message);
            _warnings.Add(message);
        }

        public void LogError(string message)
        {
            Record("Error", message);
            Assert.Fail($"Unexpected error logged: {message}");
        }

        public void LogException(Exception exception)
        {
            Record("Exception", exception.ToString());
            Assert.Fail($"Unexpected exception logged: {exception}");
        }

        void Record(string level, string message)
        {
            _messages.Add(message);
            TestContext.WriteLine($"[{level}] {message}");
        }
    }
}