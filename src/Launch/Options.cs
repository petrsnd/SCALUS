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

namespace OneIdentity.Scalus.Launch
{
    using System;
    using System.CommandLine;

    public class Options : IVerb
    {
        public string Url { get; set; }

        public bool Preview { get; set; }

        public bool Debug { get; set; }

        public Command CreateCommand(Action<object> onParsed)
        {
            var url = new Option<string>("--url", "-u") { Description = "The URL to launch.", Required = true };
            var preview = new Option<bool>("--preview", "-p") { Description = "Show me what will launch, but dont run it. This will also report the token values and show the contents of the generated file, if applicable." };
            var debug = new Option<bool>("--debug") { Description = "Keep the console window visible for troubleshooting (by default the launch verb runs windowless)." };
            var command = new Command("launch", "Launch an app configured for the specified URL");
            command.Add(url);
            command.Add(preview);
            command.Add(debug);
            command.SetAction(result =>
            {
                onParsed(new Options { Url = result.GetValue(url), Preview = result.GetValue(preview), Debug = result.GetValue(debug) });
                return 0;
            });
            return command;
        }
    }
}
