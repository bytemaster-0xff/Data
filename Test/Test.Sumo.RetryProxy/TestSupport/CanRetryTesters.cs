﻿using Sumo.Retry;
using System;

namespace Sumo.Data.Retryupport
{
    public class CanRetryProxySubjectException : ICanRetryTester
    {
        public bool CanRetry(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            return exception is ProxySubjectException;
        }
    }

    public class CanNotRetryProxySubjectException : ICanRetryTester
    {
        public bool CanRetry(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            return !(exception is ProxySubjectException);
        }
    }
}