using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ActiveMQ_WpfAppConsumer
{
    public partial class MainWindow : Window
    {
        private ActiveMQConsumer consumer;
        private List<(ComboBox TopicBox, TextBox FieldsBox)> topicFieldEntries;
        List<string> topics;
        private ActiveMQTopicsFetcher topicsFetcher = new ActiveMQTopicsFetcher();
        private string hostname;

        public MainWindow()
        {
            InitializeComponent();
            StopButton.IsEnabled = false; 
            topicFieldEntries = new List<(ComboBox TopicBox, TextBox FieldsBox)>();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private void Connect()
        {
            hostname = UriTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(hostname))
            {
                Statuslbl.Content = "connecting...";
                consumer = new ActiveMQConsumer($"activemq:tcp://{hostname}:61616"); 
                //consumer = new ActiveMQConsumer($"activemq:tcp://{hostname}:61616");   //  THIS IS THE MAIN FUNCTION
                if (consumer.is_connected)
                {
                    Statuslbl.Content = "connected.";
                    ConnectButton.IsEnabled = false;
                    LoadActiveMQTopicsAsync();
                }
                else { MessageBox.Show("could not connect, maybe check firewall?"); }
            }
        }

        private async void LoadActiveMQTopicsAsync()
        {
            string jolokiaUrl = $"http://{hostname}:8161/api/jolokia/read/org.apache.activemq:type=Broker,brokerName=amq-broker,destinationType=Topic,*";
            //string jolokiaUrl = $"http://{hostname}:8181/jolokia/read/org.apache.activemq:type=Broker,brokerName=amq-broker,destinationType=Topic,*";
            Statuslbl.Content = "loading topics...";
            topics = (await topicsFetcher.GetActiveMQTopicsAsync(jolokiaUrl)).OrderBy(t => t).ToList();
            Statuslbl.Content = "topics added.";
            AddTopicFieldEntry();
        }


        private void AddTopicField_Click(object sender, RoutedEventArgs e)
        {
            AddTopicFieldEntry();
        }

        private void AddTopicFieldEntry()
        {
            var topicComboBox = new ComboBox() { AllowDrop = true, Margin = new Thickness(5, 1, 5, 1), Height = 20, Width = 200 };
            foreach (var topic in topics)
            {
                topicComboBox.Items.Add(topic);
            }
            var fieldsTextBox = new TextBox { Margin = new Thickness(5, 1, 5, 1), Height = 20, Width = 245 };

            var topicLabel = new Label { Content = "Topic:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 1, 5, 1) };
            var fieldsLabel = new Label { Content = "Fields:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 1, 5, 1) };

            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(topicLabel);
            panel.Children.Add(topicComboBox);
            panel.Children.Add(fieldsLabel);
            panel.Children.Add(fieldsTextBox);

            TopicFieldStackPanel.Children.Add(panel);

            topicFieldEntries.Add((topicComboBox, fieldsTextBox));
        }


        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Connect();
            consumer.OnMessageReceived += Consumer_OnMessageReceived;   //  THIS IS THE MAIN FUNCTION
            Statuslbl.Content = "attached to event handler.";
            foreach (var (TopicBox, FieldsBox) in topicFieldEntries)
            {
                var topic = TopicBox.Text.Trim();
                var fields = FieldsBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(f => f.Trim()).ToArray();
                if (!string.IsNullOrEmpty(topic))
                {
                    Statuslbl.Content = $"subscribed to {topicFieldEntries.Count} topics";
                    consumer.SubscribeToTopic(topic, fields);  //  THIS IS THE MAIN FUNCTION
                }
            }

            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            UriTextBox.IsEnabled = false;
        }

        private void Consumer_OnMessageReceived(object sender, string message)
        {
            Dispatcher.Invoke(() => MessagesListBox.Items.Insert(0, $"{System.DateTime.Now}: {message}"));
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Statuslbl.Content = $"stopped listening.";
            consumer?.Close();

            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            UriTextBox.IsEnabled = true;
            ConnectButton.IsEnabled = true;
            Statuslbl.Content = $"disconnected.";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            consumer?.Close();
        }
    }
}
