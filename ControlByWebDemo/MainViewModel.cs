using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;
using ControlByWebDemo.Utils;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace ControlByWebDemo
{
    public class MainViewModel : ViewModelBase
    {
        #region Constructors And Destructors

        public MainViewModel()
        {
            IpAddress = "10.1.0.99";
            PortNum = 80;
            SendCommandString = GetBasicStateString();

            SendCommandToDeviceCommand = new RelayCommand(SendCommandToDevice);
            ClearTextCommand = new RelayCommand(ClearText);
            ResetSendTextToGetStateCommand = new RelayCommand(ResetSendTextToGetState);

            SetRelayOnCommand = new RelayCommand<int>(SetRelayOn);
            SetRelayOffCommand = new RelayCommand<int>(SetRelayOff);
        }

        #endregion

        #region Commands

        public ICommand ClearTextCommand { get; set; }

        public ICommand ResetSendTextToGetStateCommand { get; set; }

        public ICommand SendCommandToDeviceCommand { get; set; }

        public ICommand SetRelayOffCommand { get; set; }

        public ICommand SetRelayOnCommand { get; set; }

        #endregion

        #region Properties

        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                RaisePropertyChanged();
            }
        }

        public int PortNum
        {
            get => _portNum;

            set
            {
                _portNum = value;
                RaisePropertyChanged();
            }
        }

        public string ReceivedDataString
        {
            get => _receivedDataString;

            set
            {
                _receivedDataString = value;
                RaisePropertyChanged();
            }
        }

        public string SendCommandString
        {
            get => _sendCommandString;

            set
            {
                _sendCommandString = value;
                RaisePropertyChanged();
            }
        }

        private bool _isMessageJsonFormat;

        public bool IsMessageJsonFormat
        {
            get => _isMessageJsonFormat;

            set
            {
                _isMessageJsonFormat = value;
                RaisePropertyChanged();
            }
        }


        #endregion

        #region Non-Public Methods

        private void ClearText()
        {
            SendCommandString = "";
        }


        private string GetSetterStringForRelay(int relayNum, bool isOn)
        {
            var relayCode = isOn ? 1 : 0;

            string formatCode = IsMessageJsonFormat ? ".json" : ".xml";

            var setterString = $"GET /state{formatCode}?relay{relayNum}={relayCode} HTTP/1.1{LineTerminationString}Authorization: Basic bm9uZTp3ZWJyZWxheQ=={LineTerminationString}{LineTerminationString}";
            return setterString;
        }


        private string GetBasicStateString()
        {
            string formatCode = IsMessageJsonFormat ? ".json" : ".xml";
            var commandString = $"GET /state{formatCode}? HTTP/1.1{LineTerminationString}Authorization: Basic bm9uZTp3ZWJyZWxheQ=={LineTerminationString}{LineTerminationString}";
            return commandString;
        }


        private void ResetReceivedString()
        {
            ReceivedDataString = "";
        }

        private void ResetSendTextToGetState()
        {
            SendCommandString = GetBasicStateString();
            SendCommandToDevice();
        }

        private void SendCommandToDevice()
        {
            var port = PortNum;
            var ipAddress = IpAddress;

            var tcpClient = new TcpClient();

            var sendCommandString = SendCommandString;

            ResetReceivedString();


            try
            {
                tcpClient.Connect(ipAddress, port);

                var stream = tcpClient.GetStream();

                if (stream.CanWrite && stream.CanRead)
                {
                    var sendBytes = Encoding.ASCII.GetBytes(sendCommandString);

                    stream.Write(sendBytes, 0, sendBytes.Length);

                    var numBytes = tcpClient.ReceiveBufferSize;
                    var receiveBytes = new byte[numBytes];

                    stream.Read(receiveBytes, 0, numBytes);

                    var receivedString = Encoding.ASCII.GetString(receiveBytes);
                    ReceivedDataString = receivedString;
                }
            }
            catch (Exception exception)
            {
                DebugUtils.Write($"Exception! -- {exception.Message}");

                ReceivedDataString = exception.Message;
            }
            finally
            {
                //Is this always safe to do?
                tcpClient.Close();
            }
        }

        private void SetRelayOff(int relayNum)
        {
            var setString = GetSetterStringForRelay(relayNum, false);
            SendCommandString = setString;
            SendCommandToDevice();
        }

        private void SetRelayOn(int relayNum)
        {
            var setString = GetSetterStringForRelay(relayNum, true);
            SendCommandString = setString;
            SendCommandToDevice();
        }

        #endregion

        #region Fields

        private const string LineTerminationString = "\r\n";

        private string _ipAddress;
        private int _portNum;
        private string _receivedDataString;


        private string _sendCommandString;

        #endregion
    }
}