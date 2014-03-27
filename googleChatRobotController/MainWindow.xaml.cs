using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;
using System.Windows.Threading;
using agsXMPP;

namespace googleChatRobotController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        XmppClientConnection googleChat = new XmppClientConnection();

        private Dispatcher _dispatcher;

        private System.IO.Ports.SerialPort serialPort;
        
        bool isAssistedNavigation = false;

        public MainWindow()
        {
            InitializeComponent();

            _dispatcher = this.Dispatcher;

            listGoogleChatEvents.Items.Clear();

            // Subscribe to Events
            googleChat.OnLogin += new ObjectHandler(googleChat_OnLogin);
            googleChat.OnRosterStart += new ObjectHandler(googleChat_OnRosterStart);
            googleChat.OnRosterEnd += new ObjectHandler(googleChat_OnRosterEnd);
            googleChat.OnRosterItem += new XmppClientConnection.RosterHandler(googleChat_OnRosterItem);
            googleChat.OnPresence += new agsXMPP.protocol.client.PresenceHandler(googleChat_OnPresence);
            googleChat.OnAuthError += new XmppElementHandler(googleChat_OnAuthError);
            googleChat.OnError += new ErrorHandler(googleChat_OnError);
            googleChat.OnClose += new ObjectHandler(googleChat_OnClose);
            googleChat.OnMessage += new agsXMPP.protocol.client.MessageHandler(googleChat_OnMessage);

            //availabe COM ports
            SerialPort tmp;
            foreach (string str in SerialPort.GetPortNames())
            {
                tmp = new SerialPort(str);
                if (tmp.IsOpen == false)
                    comboBoxSerialPort.Items.Add(str);
            }

            serialPort = new SerialPort();

            Properties.Settings.Default.Reload();
            passwordBoxGoogleChat.Password = Properties.Settings.Default.password;
            textBoxGoogleChatUsername.Text = Properties.Settings.Default.email;

            if (textBoxGoogleChatUsername.Text != null && textBoxGoogleChatUsername.Text != "" && passwordBoxGoogleChat.Password != null && passwordBoxGoogleChat.Password != "")
            {
                buttonGoogleChatSignIn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }


        #region Sign In
        private void buttonSignIn_Click(object sender, RoutedEventArgs e)
        {
            String email = "";

            if (textBoxGoogleChatUsername.Text.Contains('@'))
            {
                if (textBoxGoogleChatUsername.Text.Contains("@gmail.com"))
                {
                    email = textBoxGoogleChatUsername.Text;
                }
                else
                {
                    MessageBox.Show(
                    @"This only works with gmail accounts.
Sorry!",
                    "Not supported e-mail",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                    return;
                }
            }
            else
            {
                if (textBoxGoogleChatUsername.Text != "")
                {
                    email = textBoxGoogleChatUsername.Text += "@gmail.com";
                }
                else
                {
                    MessageBox.Show(
                    @"Please fill the textboxes before.",
                    "Not supported e-mail",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                    return;
                }
            }

            Jid googleChatUser = new Jid(email);

            googleChat.Username = googleChatUser.User;

            googleChat.Server = googleChatUser.Server;

            googleChat.Password = passwordBoxGoogleChat.Password;
            googleChat.AutoResolveConnectServer = true;

            googleChat.Open();

            textBoxGoogleChatUsername.IsEnabled = false;
            passwordBoxGoogleChat.IsEnabled = false;

            buttonGoogleChatSignIn.IsEnabled = false;
            checkBoxGoogleChatRememberMe.IsEnabled = false;
            buttonGoogleChatSignOut.IsEnabled = true;

            expanderGoogleChat.IsExpanded = true;
            expanderGoogleChatSignIn.IsExpanded = false;

            if (checkBoxGoogleChatRememberMe.IsChecked == true)
            {
                Properties.Settings.Default.password = passwordBoxGoogleChat.Password;
                Properties.Settings.Default.email = textBoxGoogleChatUsername.Text;

                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.password = "";
                Properties.Settings.Default.email = "";

                Properties.Settings.Default.Save();
            }
        }

        private void buttonSignOut_Click(object sender, RoutedEventArgs e)
        {
            // close the xmpp connection
            googleChat.Close();

            textBoxGoogleChatUsername.IsEnabled = true;
            passwordBoxGoogleChat.IsEnabled = true;

            buttonGoogleChatSignIn.IsEnabled = true;
            checkBoxGoogleChatRememberMe.IsEnabled = true;
            buttonGoogleChatSignOut.IsEnabled = false;

            expanderGoogleChat.IsExpanded = false;

        }
        #endregion


        #region Google Chat
        private void googleChat_OnClose(object sender)
        {
            _dispatcher.BeginInvoke((Action)(() => {
                listGoogleChatEvents.Items.Add("OnClose Connection closed");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnError(object sender, Exception ex)
        {
            _dispatcher.BeginInvoke((Action)(() => {
                listGoogleChatEvents.Items.Add("OnError");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnAuthError(object sender, agsXMPP.Xml.Dom.Element e)
        {
            _dispatcher.BeginInvoke((Action)(() => {
                listGoogleChatEvents.Items.Add("OnAuthError");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnRosterItem(object sender, agsXMPP.protocol.iq.roster.RosterItem item)
        {
            _dispatcher.BeginInvoke((Action)(() => { 
                //listGoogleChatEvents.Items.Add(String.Format("Received Contact {0}", item.Jid.Bare));
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));

        }

        private void googleChat_OnRosterEnd(object sender)
        {
           _dispatcher.BeginInvoke((Action)(() => {
               listGoogleChatEvents.Items.Add("OnRosterEnd");
               listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));

            // Send our own presence to teh server, so other epople send us online
            // and the server sends us the presences of our contacts when they are
            // available
            googleChat.SendMyPresence();
        }

        private void googleChat_OnRosterStart(object sender)
        {
            _dispatcher.BeginInvoke((Action)(() => {
                listGoogleChatEvents.Items.Add("OnRosterStart");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnLogin(object sender)
        {
            _dispatcher.BeginInvoke((Action)(() => {
                listGoogleChatEvents.Items.Add("OnLogin");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnMessage(object sender, agsXMPP.protocol.client.Message msg)
        {
            // ignore empty messages (events)
            if (msg.Body == null || msg.From.Bare == "")
                return;

            _dispatcher.BeginInvoke((Action)(() =>
            { 
                listGoogleChatEvents.Items.Add(String.Format("Message from {1}: {2}", msg.From.User, msg.From.Bare, msg.Body));
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));

            _dispatcher.BeginInvoke((Action)(() =>
            {
                int sameItemNumber = 0;
                int i = 0;
                while (i < comboBoxSelectedUser.Items.Count)
                {
                    if (msg.From.Bare == comboBoxSelectedUser.Items.GetItemAt(i).ToString())
                        sameItemNumber++;

                    i++;
                }

                if (sameItemNumber == 0) // New chat
                {
                    comboBoxSelectedUser.Items.Add(msg.From.Bare);

                    if (checkBoxSendWelcomeMessage.IsChecked == true)
                    {
                        agsXMPP.protocol.client.Message answerMsg = new agsXMPP.protocol.client.Message();
                        answerMsg.Type = agsXMPP.protocol.client.MessageType.chat;
                        answerMsg.To = new Jid(msg.From.Bare);
                        answerMsg.Body = Properties.Settings.Default.welcomeMessage;
                        googleChat.Send(answerMsg);
                    }
                }

                if (comboBoxSelectedUser.SelectedItem == null) // First chat
                    comboBoxSelectedUser.SelectedItem = msg.From.Bare;


                if (msg.From.Bare == comboBoxSelectedUser.SelectedItem.ToString() && msg.From.Bare != "") // Message from the selected user
                {
                    if (msg.Body.Contains("http"))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(msg.Body);
                        }
                        catch { }
                    }


                    if (serialPort.IsOpen == true)
                    {
                        if (msg.Body.Contains('2')) 
                            isAssistedNavigation = false;
                        else if (msg.Body.Contains('1'))
                            isAssistedNavigation = true;

                        if (!msg.Body.Contains('1') && !msg.Body.Contains('2') && isAssistedNavigation == false)
                        {
                            serialPort.WriteLine("1");
                            textBoxSerial.Text += string.Format("S: {0} \r\n", '1');
                            isAssistedNavigation = true;
                        }


                        serialPort.Write(msg.Body.ToCharArray(), 0, 1);
                        textBoxSerial.Text += string.Format("S: {0} \r\n", msg.Body);
                    }
                    else
                    {
                        if (!msg.Body.Contains('1') && !msg.Body.Contains('2') && isAssistedNavigation == false)
                        {
                            if (serialPort.IsOpen == true) serialPort.WriteLine("1");
                            textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", '1');
                            //isAssistedNavigation = true;
                        }

                        if (checkBoxSendFailedToSendMessage.IsChecked == true)
                        {
                            textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg.Body);
                            agsXMPP.protocol.client.Message answerMsg = new agsXMPP.protocol.client.Message();
                            answerMsg.Type = agsXMPP.protocol.client.MessageType.chat;
                            answerMsg.To = new Jid(msg.From.Bare);
                            answerMsg.Body = Properties.Settings.Default.failedToSendMessage;
                            googleChat.Send(answerMsg);
                        }
                    }
                }
                else // Message from other User
                {
                    if (checkBoxSendWaitMessage.IsChecked == true)
                    {
                        agsXMPP.protocol.client.Message answerMsg = new agsXMPP.protocol.client.Message();
                        answerMsg.Type = agsXMPP.protocol.client.MessageType.chat;
                        answerMsg.To = new Jid(msg.From.Bare);
                        answerMsg.Body = Properties.Settings.Default.waitMessage;
                        googleChat.Send(answerMsg);
                    }
                }

            }));
        }

        private void googleChat_OnPresence(object sender, agsXMPP.protocol.client.Presence pres)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                String email = pres.From.ToString();
                //email = pres.From.Bare;

                int sameItemNumber = 0;
                int i = 0;
                while (i < comboBoxGoogleChatOnlineFriends.Items.Count)
                {
                    if (email == comboBoxGoogleChatOnlineFriends.Items.GetItemAt(i).ToString())
                        sameItemNumber++;

                    i++;
                }

                if (sameItemNumber == 0)
                {
                    comboBoxGoogleChatOnlineFriends.Items.Add(email);
                }

                //listGoogleChatEvents.Items.Add(String.Format("Received Presence from:{0} user:{1} server:{2}", pres.From.Bare, pres.From.User, pres.From.Server));// .Status));
                
            }));
        }
        #endregion


        #region Serial Port
        private void comboBoxSerialPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                serialPort.PortName = comboBoxSerialPort.SelectedItem.ToString();

                //open serial port
                serialPort.Open();
                comboBoxSerialPort.IsEnabled = false;
                buttonCloseSerialPort.IsEnabled = true;
                buttonFindSerialPort.IsEnabled = false;
                buttonOpenSerialPort.IsEnabled = false;
                
                serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

                expanderControls.IsExpanded = true;
                expanderSerialPort.IsExpanded = false;
            }));
        }

        private void buttonFindSerialPort_Click(object sender, RoutedEventArgs e)
        {
            //availabe COM ports
            SerialPort tmp;
            foreach (string str in SerialPort.GetPortNames())
            {
                tmp = new SerialPort(str);
                    
                int sameItemNumber = 0;
                int i = 0;
                while(i < comboBoxSerialPort.Items.Count)
                {
                    if(str == comboBoxSerialPort.Items.GetItemAt(i).ToString())
                        sameItemNumber++;

                    i++;
                }            
                
                if (sameItemNumber == 0)
                {
                    if (tmp.IsOpen == false)
                        comboBoxSerialPort.Items.Add(str);
                }
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // blocks until TERM_CHAR is received
            string msg = serialPort.ReadExisting();
            
            if (msg[0] == 'i')
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    if (comboBoxSelectedUser.SelectedItem != null && checkBoxSendHoleMessage.IsChecked == true)
                    {
                        agsXMPP.protocol.client.Message answerMsg = new agsXMPP.protocol.client.Message();
                        answerMsg.Type = agsXMPP.protocol.client.MessageType.chat;
                        answerMsg.To = new Jid(comboBoxSelectedUser.SelectedItem.ToString());
                        answerMsg.Body = Properties.Settings.Default.holeMessage;
                        googleChat.Send(answerMsg);
                    }

                    textBoxSerial.Text += string.Format("R: {0}", msg);
                    textBoxSerial.ScrollToEnd();
                }));
            }
            else if (msg[0] == 'b')
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    if (comboBoxSelectedUser.SelectedItem != null && checkBoxSendBumperMessage.IsChecked == true)
                    {
                        agsXMPP.protocol.client.Message answerMsg = new agsXMPP.protocol.client.Message();
                        answerMsg.Type = agsXMPP.protocol.client.MessageType.chat;
                        answerMsg.To = new Jid(comboBoxSelectedUser.SelectedItem.ToString());
                        answerMsg.Body = Properties.Settings.Default.bumperMessage;
                        googleChat.Send(answerMsg);
                    }

                    textBoxSerial.Text += string.Format("R: {0}", msg);
                    textBoxSerial.ScrollToEnd();
                }));
            }  
        }

        private void buttonCloseSerialPort_Click(object sender, RoutedEventArgs e)
        {
            serialPort.Close();

            _dispatcher.BeginInvoke((Action)(() =>
            {
                serialPort.Close();

                buttonCloseSerialPort.IsEnabled = false;
                buttonFindSerialPort.IsEnabled = true;
                buttonOpenSerialPort.IsEnabled = true;
                comboBoxSerialPort.IsEnabled = true;
                expanderControls.IsExpanded = false;
            }));

            
        }

        private void buttonOpenSerialPort_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                serialPort.PortName = comboBoxSerialPort.SelectedItem.ToString();

                //open serial port
                serialPort.Open();
                
                comboBoxSerialPort.IsEnabled = false;
                buttonCloseSerialPort.IsEnabled = true;
                buttonOpenSerialPort.IsEnabled = false;
                buttonFindSerialPort.IsEnabled = false;

                serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

                expanderControls.IsExpanded = true;
                expanderSerialPort.IsExpanded = false;
            }));
        }

        private void textBoxSerial_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxSerial.ScrollToEnd();
        }

        private void listGoogleChatEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //listGoogleChatEvents.;
            if (listGoogleChatEvents.Items.Count > 1)
            {
                //listGoogleChatEvents.ScrollIntoView(listGoogleChatEvents.Items.GetItemAt(0));
            }
        }

        private void checkBoxAutoMessages_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxAutoMessages_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void comboBoxSelectedUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                if (checkBoxSendControllerMessage.IsChecked == true)
                {
                    agsXMPP.protocol.client.Message answerMsg = new agsXMPP.protocol.client.Message();
                    answerMsg.Type = agsXMPP.protocol.client.MessageType.chat;
                    answerMsg.To = new Jid(comboBoxSelectedUser.Text);
                    answerMsg.Body = Properties.Settings.Default.controllerMessage;
                    googleChat.Send(answerMsg);
                }

                buttonUncheckSelectedUser.IsEnabled = true;
            }));
        }

        private void buttonUncheckSelectedUser_Click(object sender, RoutedEventArgs e)
        {
            buttonUncheckSelectedUser.IsEnabled = false;
            comboBoxSelectedUser.SelectedIndex = -1;
        }
        #endregion


        #region Send Message
        private void buttonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            // Send a message
            agsXMPP.protocol.client.Message msg = new agsXMPP.protocol.client.Message();
            msg.Type = agsXMPP.protocol.client.MessageType.chat;
            //msg.To = new Jid(txtJabberIdReceiver.Text);
            msg.To = new Jid(comboBoxGoogleChatOnlineFriends.SelectedItem.ToString());
            msg.Body = textBoxMessage.Text;

            googleChat.Send(msg);

            textBoxMessage.Text = "";
        }

        private void comboBoxOnlineFriends_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonSendMessage.IsEnabled = true;
        }
        #endregion


        #region Controls
        private void buttonForward_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    String msg = "1";
                    if (serialPort.IsOpen)
                    {
                        serialPort.WriteLine(msg);
                        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                    }
                    else
                    {
                        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                    }
                }));

                isAssistedNavigation = true;
            }            

             _dispatcher.BeginInvoke((Action)(() =>
            {
                String msg = "w";
                if (serialPort.IsOpen)
                {
                    serialPort.WriteLine(msg);
                    textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                }
                else
                {
                    textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                }
            }));
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    String msg = "1";
                    if (serialPort.IsOpen)
                    {
                        serialPort.WriteLine(msg);
                        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                    }
                    else
                    {
                        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                    }
                }));

                isAssistedNavigation = true;
            }

            _dispatcher.BeginInvoke((Action)(() =>
            {
                String msg = "p";
                if (serialPort.IsOpen)
                {
                    serialPort.WriteLine(msg);
                    textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                }
                else
                {
                    textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                }
            }));
        }

        private void buttonBackward_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    String msg = "1";
                    if (serialPort.IsOpen)
                    {
                        serialPort.WriteLine(msg);
                        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                    }
                    else
                    {
                        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                    }
                }));

                isAssistedNavigation = true;
            }

            _dispatcher.BeginInvoke((Action)(() =>
            {
                String msg = "s";
                if (serialPort.IsOpen)
                {
                    serialPort.WriteLine(msg);
                    textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                }
                else
                {
                    textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                }
            }));

        }

        private void buttonLeft_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    String msg = "1";
                    if (serialPort.IsOpen)
                    {
                        serialPort.WriteLine(msg);
                        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                    }
                    else
                    {
                        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                    }
                }));

                isAssistedNavigation = true;
            }

            _dispatcher.BeginInvoke((Action)(() =>
            {
                String msg = "a";
                if (serialPort.IsOpen)
                {
                    serialPort.WriteLine(msg);
                    textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                }
                else
                {
                    textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                }
            }));
        }

        private void buttonRight_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    String msg = "1";
                    if (serialPort.IsOpen)
                    {
                        serialPort.WriteLine(msg);
                        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                    }
                    else
                    {
                        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                    }
                }));

                isAssistedNavigation = true;
            }

            _dispatcher.BeginInvoke((Action)(() =>
            {
                String msg = "d";
                if (serialPort.IsOpen)
                {
                    serialPort.WriteLine(msg);
                    textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                }
                else
                {
                    textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                }
            }));
        }

        private void buttonAutonomousNavigation_Click(object sender, RoutedEventArgs e)
        {
            isAssistedNavigation = false;

            _dispatcher.BeginInvoke((Action)(() =>
            {
                String msg = "2";
                if (serialPort.IsOpen)
                {
                    serialPort.WriteLine(msg);
                    textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                }
                else
                {
                    textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                }
            }));
        }
        #endregion
     

        #region Expander Events
        private void expanderGoogleChat_Expanded(object sender, RoutedEventArgs e)
        {
            manageCollapses();
        }

        private void expanderControls_Expanded(object sender, RoutedEventArgs e)
        {
            manageCollapses();
        }

        private void expanderGoogleChat_Collapsed(object sender, RoutedEventArgs e)
        {
            manageCollapses();
        }

        private void expanderControls_Collapsed(object sender, RoutedEventArgs e)
        {
            manageCollapses();
        }

        private void expanderGoogleChatSignIn_Expanded(object sender, RoutedEventArgs e)
        {
            //expanderGoogleChat.Margin.Top = expanderSignIn.Margin.Top + expanderSignIn.Height;
            try
            {
                Canvas.SetTop(expanderGoogleChat, expanderGoogleChatSignIn.Margin.Top + expanderGoogleChatSignIn.Height);
            }
            catch
            {
            }

            manageCollapses();
        }

        private void expanderGoogleChatSignIn_Collapsed(object sender, RoutedEventArgs e)
        {
            try
            {
                Canvas.SetTop(expanderGoogleChat, expanderGoogleChatSignIn.Margin.Top + 50);
            }
            catch
            {
            }

            manageCollapses();
        }

        private void expanderSerialPort_Expanded(object sender, RoutedEventArgs e)
        {
            //expanderGoogleChat.Margin.Top = expanderSignIn.Margin.Top + expanderSignIn.Height;
            try
            {
                Canvas.SetTop(expanderControls, expanderSerialPort.Margin.Top + expanderSerialPort.Height);
            }
            catch
            {
            }

            manageCollapses();
        }

        private void expanderSerialPort_Collapsed(object sender, RoutedEventArgs e)
        {
            try
            {
                Canvas.SetTop(expanderControls, expanderSerialPort.Margin.Top + 50);
            }
            catch
            {
            }

            manageCollapses();
        }

        private void manageCollapses()
        {
            try
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    int leftHeight = 64;
                    if (expanderSerialPort.IsExpanded == true)
                        leftHeight += (int)expanderSerialPort.Height;
                    else
                        leftHeight += 32;

                    if (expanderControls.IsExpanded == true) 
                        leftHeight += (int)expanderControls.Height;
                    else
                        leftHeight += 32;

                    int rightHeight = 64;
                    if (expanderGoogleChatSignIn.IsExpanded == true)
                        rightHeight += (int)expanderGoogleChatSignIn.Height;
                    else
                        rightHeight += 32;

                    if (expanderGoogleChat.IsExpanded == true) 
                        rightHeight += (int)expanderGoogleChat.Height;
                    else
                        rightHeight += 32;

                    if (leftHeight > rightHeight)
                    {
                        googleChatRobotController.Height = leftHeight;
                        MainCanvas.Height = googleChatRobotController.Height-40;
                    }
                    else
                    {
                        googleChatRobotController.Height = rightHeight;
                        MainCanvas.Height = googleChatRobotController.Height-40;
                    }
                }));
            }
            catch
            {
            }
        }
        #endregion
    }
        
}
