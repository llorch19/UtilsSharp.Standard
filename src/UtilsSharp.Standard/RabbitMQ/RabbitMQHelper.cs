﻿using Newtonsoft.Json;
using OptionConfig;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ
{

    /// <summary>
    /// RabbitMQ帮助类
    /// </summary>
    public class RabbitMqHelper
    {
        private static ConnectionFactory _rabbitConnectionFactory;
        private static IConnection _rabbitConnection;
        private static string _rabbitMqAddress;
        private static TimeSpan _requestedConnectionTimeout;
        private static TimeSpan _requestedHeartbeat;
        private static bool? _automaticRecoveryEnabled;


        /// <summary>
        /// 构造函数
        /// </summary>
        public RabbitMqHelper()
        {
            var rabbitMqConfig = RabbitMqConfig.RabbitMqSetting;
            if (rabbitMqConfig == null)
            {
                throw new Exception("rabbitMqConfig cannot be null");
            }
            if (string.IsNullOrEmpty(rabbitMqConfig.RabbitMqConnection))
            {
                throw new Exception("rabbitMqConnection cannot be null or empty");
            }
            _rabbitMqAddress = rabbitMqConfig.RabbitMqConnection;
            _requestedConnectionTimeout = rabbitMqConfig.RequestedConnectionTimeout;
            _requestedHeartbeat = rabbitMqConfig.RequestedHeartbeat;
            _automaticRecoveryEnabled = rabbitMqConfig.AutomaticRecoveryEnabled;
            Init();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private static void Init()
        {
            _rabbitConnectionFactory = new ConnectionFactory() { Uri = new Uri(_rabbitMqAddress) };
            
            if (_requestedConnectionTimeout != default)
            {
                _rabbitConnectionFactory.RequestedConnectionTimeout = _requestedConnectionTimeout.Milliseconds;
            }
            if (_requestedHeartbeat != default)
            {
                _rabbitConnectionFactory.RequestedHeartbeat =(ushort)_requestedHeartbeat.Seconds;
            }
            if (_automaticRecoveryEnabled != null)
            {
                _rabbitConnectionFactory.AutomaticRecoveryEnabled = _automaticRecoveryEnabled.Value;
            }
            _rabbitConnection = _rabbitConnectionFactory.CreateConnection();

        }

        /// <summary>
        /// 获取Channel
        /// </summary>
        /// <returns></returns>
        public static IModel GetChannel()
        {
            if (!_rabbitConnection.IsOpen)
            {
                Init();
            }
            return _rabbitConnection.CreateModel();
        }


        /// <summary>
        /// 获取消费者数量
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <returns></returns>
        public uint GetConsumerCount(string queueName)
        {
            using var channel = GetChannel();
            return channel.ConsumerCount(queueName);
        }

        /// <summary>
        /// 获取消息数量
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <returns></returns>
        public uint GetMessageCount(string queueName)
        {
            using var channel = GetChannel();
            return channel.MessageCount(queueName);
        }

        /// <summary>
        /// 申明交换机
        /// </summary>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="exchangeType">交换机类型</param>
        public void ExchangeDeclare(string exchangeName, string exchangeType = ExchangeType.Direct)
        {
            using var channel = GetChannel();
            channel.ExchangeDeclare(exchangeName, exchangeType, true, false, null);
        }

        /// <summary>
        /// 申明队列
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="arguments">参数</param>
        public void QueueDeclare(string queueName, IDictionary<string, object> arguments=null)
        {
            using var channel = GetChannel();
            channel.QueueDeclare(queueName, true, false, false, arguments);
        }

        /// <summary>
        /// 删除队列
        /// </summary>
        /// <param name="queueName"></param>
        public void QueueDelete(string queueName)
        {
            using var channel = GetChannel();
            channel.QueueDelete(queue: queueName);
        }

        /// <summary>
        /// 队列绑定
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="routingKey">路由key</param>
        public void QueueBind(string queueName,string exchangeName, string routingKey)
        {
            using var channel = GetChannel();
            channel.QueueBind(queueName, exchangeName, routingKey, null);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="routingKey">路由key</param>
        /// <param name="content">消息内容</param>
        public void Send<T>(string exchangeName, string routingKey,T content) where T : class
        {
            if (content == null) return;
            var message = content.GetType().Name != "String" ? JsonConvert.SerializeObject(content) : content.ToString();
            using var channel = GetChannel();
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.DeliveryMode = 2;
            byte[] body = Encoding.UTF8.GetBytes(message);
            //开始发送
            channel.BasicPublish(exchangeName, routingKey, properties, body);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="routingKey">路由key</param>
        /// <param name="contents">消息内容集合</param>
        public void Send<T>(string exchangeName, string routingKey, List<T> contents) where T : class
        {
            if (contents == null|| contents.Count ==0) return;
            var type = contents.First().GetType().Name;
            using var channel = GetChannel();
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;//是否持久化
            properties.DeliveryMode = 2;
            foreach (var content in contents)
            {
                var message = type != "String" ? JsonConvert.SerializeObject(content) : content.ToString();
                byte[] body = Encoding.UTF8.GetBytes(message);
                //开始发送
                channel.BasicPublish(exchangeName, routingKey, properties, body);
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="routingKey">路由key</param>
        /// <param name="content">消息内容</param>
        /// <param name="expiration">过期时间（秒）</param>
        public void Send<T>(string exchangeName, string routingKey, T content, int expiration) where T : class
        {
            if (content == null) return;
            expiration = expiration * 1000;
            var message = content.GetType().Name != "String" ? JsonConvert.SerializeObject(content) : content.ToString();
            using var channel = GetChannel();
            var properties = channel.CreateBasicProperties();
            properties.Expiration = expiration.ToString();
            byte[] body = Encoding.UTF8.GetBytes(message);
            //开始发送
            channel.BasicPublish(exchangeName, routingKey, properties, body);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="routingKey">路由key</param>
        /// <param name="contents">消息内容集合</param>
        /// <param name="expiration">过期时间（秒）</param>
        public void Send<T>(string exchangeName, string routingKey, List<T> contents, int expiration) where T : class
        {
            if (contents == null || contents.Count == 0) return;
            expiration = expiration * 1000;
            var type = contents.First().GetType().Name;
            using var channel = GetChannel();
            var properties = channel.CreateBasicProperties();
            properties.Expiration = expiration.ToString();
            foreach (var content in contents)
            {
                var message = type != "String" ? JsonConvert.SerializeObject(content) : content.ToString();
                byte[] body = Encoding.UTF8.GetBytes(message);
                //开始发送
                channel.BasicPublish(exchangeName, routingKey, properties, body);
            }
        }

        /// <summary>
        /// 消费者接收消息
        /// </summary>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="routingKey">路由key</param>
        /// <param name="queueName">队列名称</param>
        /// <param name="callback">回调函数</param>
        /// <param name="exchangeType">交换机类型，默认：ExchangeType.Direct</param>
        /// <param name="errorCallback">错误回调函数</param>
        public void Received(string exchangeName, string routingKey, string queueName, Action<string> callback, string exchangeType = ExchangeType.Direct, Action errorCallback = null)
        {
            Task.Run(() =>
            {
                using var channel = GetChannel();
                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body);
                    try
                    {
                        callback.Invoke(message);
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        errorCallback?.Invoke();
                    }
                };
                channel.BasicConsume(queueName, autoAck: false, consumer: consumer);
            });
        }

        /// <summary>
        /// 消费者批量接收消息
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="callback">回调函数</param>
        /// <param name="batchCount">每次批量接收条数</param>
        /// <param name="errorCallback">错误回调函数</param>
        public void BatchReceived(string queueName, Action<List<string>> callback, int batchCount = 50, Action errorCallback = null)
        {
            Task.Run(() =>
            {
                var rabbitMessage = new List<MessageAskModel>();
                while (true)
                {
                    try
                    {
                        var data = GetMessage(queueName);
                        if (data == null)
                        {
                            BatchReceivedHandle(callback, rabbitMessage);
                            rabbitMessage = new List<MessageAskModel>();
                            Thread.Sleep(100);
                            continue;
                        }
                        rabbitMessage.Add(data);
                        if (rabbitMessage.Count < batchCount) continue;
                        BatchReceivedHandle(callback, rabbitMessage);
                        rabbitMessage = new List<MessageAskModel>();
                    }
                    catch (Exception ex)
                    {
                        BatchReceivedHandle(callback, rabbitMessage);
                        rabbitMessage = new List<MessageAskModel>();
                    }
                }
            });
        }

        /// <summary>
        /// 获取消息
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="autoAck">是否消息自动确认</param>
        /// <returns></returns>
        private MessageAskModel GetMessage(string queueName, bool autoAck = true)
        {
            using var channel = GetChannel();
            if (channel.MessageCount(queueName) == 0) return null;
            var baseResult = channel.BasicGet(queueName, autoAck);
            if (baseResult == null) return null;
            var message = new MessageAskModel()
            {
                DeliveryTag = baseResult.DeliveryTag,
                Message = Encoding.UTF8.GetString(baseResult.Body)
            };
            return message;
        }

        /// <summary>
        /// 消费者批量接收消息处理
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <param name="rabbitMessage">批量消息</param>
        private void BatchReceivedHandle(Action<List<string>> callback, List<MessageAskModel> rabbitMessage)
        {
            if (rabbitMessage != null && rabbitMessage.Count > 0)
            {
                try
                {
                    callback.Invoke(rabbitMessage.Select(p => p.Message).ToList());
                    rabbitMessage = new List<MessageAskModel>();
                }
                catch (Exception e)
                {

                }
            }
        }
    }

}
