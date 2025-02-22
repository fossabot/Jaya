﻿
using System.Composition;

namespace Jaya.Shared.Services
{
    [Export(nameof(CommandService), typeof(IService))]
    public sealed class CommandService: IService
    {
        public CommandService()
        {
            EventAggregator = new EventAggregator();
        }

        public EventAggregator EventAggregator { get; }
    }
}
