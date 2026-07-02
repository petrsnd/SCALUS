// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandLineHandler.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;

    internal class CommandLineHandler : ICommandLineParser
    {
        public CommandLineHandler(IEnumerable<IVerb> verbs)
        {
            Verbs = verbs;
        }

        private IEnumerable<IVerb> Verbs { get; }

        public IApplication Build(string[] args, Func<object, IApplication> applicationResolver, out int exitCode)
        {
            object parsed = null;
            var root = new RootCommand("Session Client Application Launch Uri System (SCALUS)");
            foreach (var verb in Verbs)
            {
                root.Add(verb.CreateCommand(o => parsed = o));
            }

            exitCode = root.Parse(args).Invoke();
            return parsed is null ? null : applicationResolver(parsed);
        }
    }
}
