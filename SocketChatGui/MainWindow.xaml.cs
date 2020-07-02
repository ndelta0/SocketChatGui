using SocketMessageData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SocketChatGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string HostString { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 8000;
        public string UsernameString { get; set; } = "Anonymous_" + new Random().Next(10000, 99999).ToString();
        public string MessageString { get; set; }

        private bool _autoScroll = true;
        private static readonly Regex _regex = new Regex("[^0-9]+");
        private bool _isConnected = false;
        private readonly Connector _connector;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _connector = new Connector();

            _connector.Connected += () =>
            {
                _isConnected = true;
                WriteToChat("[Connected]");

                MessageText.IsEnabled = true;
                SendButton.IsEnabled = true;

                ConnectButton.IsEnabled = true;
                ConnectButton.Content = "Disconnect";
            };

            _connector.Disconnected += () =>
            {
                _isConnected = false;
                WriteToChat("[Disconnected]");

                MessageText.IsEnabled = false;
                SendButton.IsEnabled = false;

                ConnectButton.IsEnabled = true;
                ConnectButton.Content = "Connect";

                HostText.IsEnabled = true;
                PortText.IsEnabled = true;
                UsernameText.IsEnabled = true;
            };

            _connector.Client.MessageReceived += (MessageReceivedEventArgs args) =>
            {
                Dispatcher.Invoke(() => WriteToChat(args.Message.Content));
            };

            MessageText.IsEnabled = false;
            SendButton.IsEnabled = false;
        }

        #region ChatBox
        private void WriteToChat(string message)
        {
            if (string.IsNullOrEmpty(ChatBox.Text))
            {
                ChatBox.AppendText(message);
            }
            else
            {
                ChatBox.AppendText("\n");
                ChatBox.AppendText(message);
            }
            ChatBox.ScrollToEnd();
        }

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)
            {
                _autoScroll = ScrollViewer.VerticalOffset == ScrollViewer.ScrollableHeight;
            }

            if (_autoScroll && e.ExtentHeightChange != 0)
            {
                ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
            }
        }
        #endregion

        private void PortText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void PortText_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                _connector.Disconnect();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(HostString))
                {
                    WriteToChat("[Host is empty]");
                    return;
                }
                if (Port > 65535 || Port < 0)
                {
                    WriteToChat("[Port must be between 0 and 65535]");
                    return;
                }
                if (string.IsNullOrWhiteSpace(UsernameString))
                {
                    WriteToChat("[Username is empty]");
                    return;
                }

                HostText.IsEnabled = false;
                PortText.IsEnabled = false;
                UsernameText.IsEnabled = false;
                ConnectButton.IsEnabled = false;
                ConnectButton.Content = "Connecting...";

                _connector.SetUp(IPAddress.Parse(HostString), Port, UsernameString);
                await _connector.TryToConnect();
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(MessageString))
            {
                var message = new Message { Command = Command.Message, Content = MessageString };
                _connector.Send(message);
                WriteToChat($"Me: {message.Content}");
                MessageText.Text = string.Empty;
                MessageString = string.Empty;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isConnected)
                _connector.Disconnect();
        }

        private void MessageText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (!string.IsNullOrWhiteSpace(MessageString))
                {
                    var message = new Message { Command = Command.Message, Content = MessageString };
                    _connector.Send(message);
                    WriteToChat($"Me: {message.Content}");
                    MessageText.Text = string.Empty;
                    MessageString = string.Empty;
                }
            }
        }
    }
}
