﻿using Bt.Lib.Common.Service.KafkaService.interfaces;

namespace ems_AuthServiceLayer.KafkaService
{
    public class KafkaNotificationService
    {
        private readonly IKafkaProducerService _kafkaProducerService;

        public async Task SubscribeServiceAsync()
        {
            await _kafkaProducerService.SendExceptionNotification(null);
            await Task.CompletedTask;
        }
    }
}