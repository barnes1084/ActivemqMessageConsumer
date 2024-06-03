using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ActiveMQ_WpfAppConsumer
{
    public class ActiveMQConsumer
    {
        // Event to notify about received messages
        public event EventHandler<string> OnMessageReceived;
        public bool is_connected;

        // A collection to keep track of consumers
        private readonly List<IMessageConsumer> consumers = new List<IMessageConsumer>();
        private IConnection connection;
        private ISession session;
        private readonly Uri connectUri;

        public ActiveMQConsumer(string uri)
        {
            this.connectUri = new Uri(uri);
            InitializeConnection();
        }

        private void InitializeConnection()
        {
            try
            {
                var connectionFactory = new ConnectionFactory(connectUri)
                {
                    UserName = "admin",
                    Password = "admin"
                };
                connection = connectionFactory.CreateConnection();
                connection.Start();
                session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
                is_connected = connection.IsStarted;
            }
            catch (Exception e)
            {
                 Log.ToFile($"Error initializing ActiveMQ connection: {e.Message}");
            }
        }

        public void SubscribeToTopic(string topic, IEnumerable<string> fields)
        {
            try
            {
                IDestination destination = session.GetTopic(topic);
                var consumer = session.CreateConsumer(destination);
                consumers.Add(consumer);

                consumer.Listener += message =>
                {
                    var messageContent = ProcessMessage(message, fields, topic);
                    // Invoke the OnMessageReceived event
                    OnMessageReceived?.Invoke(this, messageContent);
                };
            }
            catch (Exception e)
            {
                Log.ToFile($"Error subscribing to topic {topic}: {e.Message}");
            }
        }

        public void SubscribeToQueue(string queue, IEnumerable<string> fields)
        {
            try
            {
                IDestination destination = session.GetQueue(queue);
                var consumer = session.CreateConsumer(destination);
                consumers.Add(consumer);

                consumer.Listener += message =>
                {
                    var messageContent = ProcessMessage(message, fields, queue);
                    // Invoke the OnMessageReceived event
                    OnMessageReceived?.Invoke(this, messageContent);
                };
            }
            catch (Exception e)
            {
                Log.ToFile($"Error subscribing to queue {queue}: {e.Message}");
            }
        }



        private string ProcessMessage(IMessage message, IEnumerable<string> fields, string topic)
        {
            try
            {
                string messageContent = GetMessageContent(message);
                if (string.IsNullOrEmpty(messageContent))
                {
                    return $"Unsupported message type or empty message for topic {topic}.";
                }

                if (fields != null && fields.Any())
                {
                    var doc = XDocument.Parse(messageContent);
                    var fieldValues = fields.Select(field => $"{field}: {doc.Descendants(field).FirstOrDefault()?.Value ?? "Field not found"}");
                    return $"{topic}: {string.Join(", ", fieldValues)}"; 
                }
                else
                {
                    return $"{topic}: {messageContent}";
                }
            }
            catch (Exception e)
            {
                return $"Error processing message: {e.Message}";
            }
        }

        private string GetMessageContent(IMessage message)
        {
            switch (message)
            {
                case ITextMessage textMessage:
                    return textMessage.Text;
                case IBytesMessage bytesMessage:
                    byte[] data = new byte[bytesMessage.BodyLength];
                    bytesMessage.ReadBytes(data);
                    return Encoding.UTF8.GetString(data);
                default:
                    return null;
            }
        }


        public void Close()
        {
            foreach (var consumer in consumers)
            {
                consumer.Close();
            }
            session?.Close();
            connection?.Close();
        }
    }
}
