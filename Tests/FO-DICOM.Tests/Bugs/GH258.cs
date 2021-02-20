﻿// Copyright (c) 2012-2021 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom.Log;
using FellowOakDicom.Tests.Helpers;
using System;
using Xunit;

namespace FellowOakDicom.Tests.Bugs
{
    [Collection("General")]
    public class GH258
    {
        public GH258()
        {

        }

        [Fact]
        public void Log_ExceptionInFormattedString_DisplaysExceptionMessage()
        {
            var name = nameof(GH258) + "A";
            var target = NLogHelper.AssignMemoryTarget(name, @"${message}");

            ILogManager logManager = NLogManager.Instance;
            var logger = logManager.GetLogger(name);
            logger.Debug("Message: {0} {1}", new NullReferenceException(), target.Name);

            var expected = $"Message: {new NullReferenceException()} {target.Name}";
            var actual = target.Logs[0];
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Log_ExceptionNotInFormattedString_ExceptionLoggedNotIncludedInMessage()
        {
            var name = nameof(GH258) + "B";

            ILogManager logManager = NLogManager.Instance;
            var target = NLogHelper.AssignMemoryTarget(name, @"${exception} ${message}");

            var logger = logManager.GetLogger(name);
            logger.Debug("Message but no exception", new NullReferenceException());

            var expected = $"{new NullReferenceException().Message} Message but no exception";
            var actual = target.Logs[0];
            Assert.Equal(expected, actual);
        }
    }
}
