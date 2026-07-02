// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Options.cs" company="One Identity Inc.">
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

namespace OneIdentity.Scalus.Verify
{
    using System;
    using System.CommandLine;

    public class Options : IVerb
    {
        public string Path { get; set; }

        public Command CreateCommand(Action<object> onParsed)
        {
            var path = new Option<string>("--path", "-p") { Description = "Path of an alternate scalus configuration file to verify instead" };
            var command = new Command("verify", "Run a syntax check on a scalus configuration file");
            command.Add(path);
            command.SetAction(result =>
            {
                onParsed(new Options { Path = result.GetValue(path) });
                return 0;
            });
            return command;
        }
    }
}
