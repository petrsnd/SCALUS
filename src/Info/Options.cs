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

namespace OneIdentity.Scalus.Info
{
    using System;
    using System.CommandLine;

    public class Options : IVerb
    {
        public bool Dto { get; set; }

        public bool Tokens { get; set; }

        public Command CreateCommand(Action<object> onParsed)
        {
            var dto = new Option<bool>("--dto", "-d") { Description = "Show DTO description" };
            var tokens = new Option<bool>("--tokens", "-t") { Description = "Show the list of tokens" };
            var command = new Command("info", "Show information about the current scalus configuration");
            command.Add(dto);
            command.Add(tokens);
            command.SetAction(result =>
            {
                onParsed(new Options { Dto = result.GetValue(dto), Tokens = result.GetValue(tokens) });
                return 0;
            });
            return command;
        }
    }
}
