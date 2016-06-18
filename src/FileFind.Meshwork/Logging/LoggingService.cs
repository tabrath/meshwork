// 
// LoggingService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FileFind.Meshwork.Logging;

namespace Meshwork.Logging
{
    [Export(typeof(ILoggingService)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class LoggingService : ILoggingService
	{
        private readonly List<ILogger> loggers;
		
		public LoggingService()
		{
            this.loggers = new List<ILogger>();
			
            var consoleLogger = new ConsoleLogger();
			loggers.Add(consoleLogger);
			
			string consoleLogLevelEnv = System.Environment.GetEnvironmentVariable("MESHWORK_CONSOLE_LOG_LEVEL");
			if (!string.IsNullOrEmpty(consoleLogLevelEnv))
            {
				try
                {
					consoleLogger.EnabledLevel = (EnabledLoggingLevel)Enum.Parse(typeof(EnabledLoggingLevel), consoleLogLevelEnv, true);
				}
                catch {}
			}
			
			string consoleLogUseColourEnv = System.Environment.GetEnvironmentVariable("MESHWORK_CONSOLE_LOG_USE_COLOUR");
			consoleLogger.UseColour = !string.IsNullOrEmpty(consoleLogUseColourEnv) && consoleLogUseColourEnv.ToLower() == "false";
			
			string logFileEnv = System.Environment.GetEnvironmentVariable("MESHWORK_LOG_FILE");
			if (!string.IsNullOrEmpty(logFileEnv))
            {
				try
                {
					var fileLogger = new FileLogger(logFileEnv);
					loggers.Add(fileLogger);
					string logFileLevelEnv = System.Environment.GetEnvironmentVariable("MESHWORK_FILE_LOG_LEVEL");
					fileLogger.EnabledLevel = (EnabledLoggingLevel)Enum.Parse(typeof(EnabledLoggingLevel), logFileLevelEnv, true);
				}
                catch (Exception e)
                {
					this.LogError(e.ToString());
				}
			}
		}

		public bool IsLevelEnabled(LogLevel level)
		{
			var l = (EnabledLoggingLevel)level;
            return this.loggers.Any(logger => (logger.EnabledLevel & l) == l);
		}
		
		public void Log(LogLevel level, string message)
		{
			var l = (EnabledLoggingLevel)level;
            this.loggers.Where(logger => (logger.EnabledLevel & l) == l)
                .ToList().ForEach(logger => logger.Log(level, message));
		}

		public ILogger GetLogger(string name)
		{
            return this.loggers.SingleOrDefault(l => l.Name == name);
		}
		
		public void AddLogger(ILogger logger)
		{
            if (this.loggers.Any(l => l.Name == logger.Name))
                throw new Exception("There is no logger registered with the name '" + logger.Name + "'");
            
			this.loggers.Add(logger);
		}
		
		public void RemoveLogger(string name)
		{
            var logger = this.loggers.SingleOrDefault(l => l.Name == name);
			if (logger == null)
				throw new Exception ("There is no logger registered with the name '" + name + "'");
            
			this.loggers.Remove(logger);
		}
	}
}