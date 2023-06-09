﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using RabbitMQ.Client.Exceptions;
using Shared;

namespace Sender
{
    public class SenderWorker : BackgroundService
    {
        private readonly IMessageSession messageSession;
        private readonly ILogger<SenderWorker> logger;

        public SenderWorker(IMessageSession messageSession, ILogger<SenderWorker> logger)
        {
            this.messageSession = messageSession;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var round = 0;
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await messageSession.Send(new Ping {Round = round++})
                            .ConfigureAwait(false);

                        logger.LogInformation($"Message #{round}");
                    }
                    catch (AlreadyClosedException e)
                    {
                        logger.LogError(e.Message, e);
                    }
                    finally
                    {
                        await Task.Delay(3_000, stoppingToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
        }
    }
}