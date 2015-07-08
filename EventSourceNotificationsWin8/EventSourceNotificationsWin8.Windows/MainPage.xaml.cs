/*
 * COPYRIGHT LICENSE: This information contains sample code provided in source code form. You may copy, modify, and distribute
 * these sample programs in any form without payment to IBM® for the purposes of developing, using, marketing or distributing
 * application programs conforming to the application programming interface for the operating platform for which the sample code is written.
 * Notwithstanding anything to the contrary, IBM PROVIDES THE SAMPLE SOURCE CODE ON AN "AS IS" BASIS AND IBM DISCLAIMS ALL WARRANTIES,
 * EXPRESS OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, ANY IMPLIED WARRANTIES OR CONDITIONS OF MERCHANTABILITY, SATISFACTORY QUALITY,
 * FITNESS FOR A PARTICULAR PURPOSE, TITLE, AND ANY WARRANTY OR CONDITION OF NON-INFRINGEMENT. IBM SHALL NOT BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL OR CONSEQUENTIAL DAMAGES ARISING OUT OF THE USE OR OPERATION OF THE SAMPLE SOURCE CODE.
 * IBM HAS NO OBLIGATION TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS OR MODIFICATIONS TO THE SAMPLE SOURCE CODE.
 */
 
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using IBM.Worklight;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EventSourceNotificationsWin8
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        WLClient client = null;

        public static MainPage _this;
        CustomChallengeHandler ch;

        public MainPage()
        {
            this.InitializeComponent();
            _this = this;
        }

        public ChallengeHandler getChallengeHandler()
        {
            return ch;
        }

        private void ConnectServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                client = WLClient.getInstance();

                WLPush push = client.getPush();
                OnReadyToSubscribeListener myOnReadyListener = new OnReadyToSubscribeListener();
                push.onReadyToSubscribeListener = myOnReadyListener;

                ch = new CustomChallengeHandler();
                client.registerChallengeHandler((BaseChallengeHandler<JObject>)ch);
                MyRespListener mylistener = new MyRespListener(null);
                client.connect(mylistener);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }

        }

        private void IsSubscribed_Click(object sender, RoutedEventArgs e)
        {
            MainPage._this.Console.Text = "\n\nSubscription Status:" + WLClient.getInstance().getPush().isSubscribed("myPush");
        }

        private void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            WLPush push = WLClient.getInstance().getPush();
            MySubscribeListener mySubListener = new MySubscribeListener();
            push.subscribe("myPush", null, mySubListener);
        }

        private void Unsubscribe_Click(object sender, RoutedEventArgs e)
        {
            WLPush push = WLClient.getInstance().getPush();
            MyUnsubscribeListener myUnsubListener = new MyUnsubscribeListener();
            push.unsubscribe("myPush", myUnsubListener);
        }


        public class MyRespListener : WLResponseListener
        {

            MainPage page;
            public MyRespListener(MainPage mainPage)
            {
                page = mainPage;
            }

            public void onSuccess(WLResponse resp)
            {
                Debug.WriteLine("Successfully connected to server " + resp.getResponseText());
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
              async () =>
              {
                  MainPage._this.Console.Text = "\n\nSuccessfully connected to server";

              });
            }

            public void onFailure(WLFailResponse resp)
            {
                Debug.WriteLine("Failure " + resp.getErrorMsg());
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    MainPage._this.Console.Text = resp.getErrorMsg();
                });
            }
        }

        public class CustomChallengeHandler : ChallengeHandler
        {
            String username;
            String password;

            public CustomChallengeHandler()
                : base("SampleAppRealm")
            { }

            public override void handleChallenge(JObject response)
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                  async () =>
                  {
                      MainPage._this.LoginGrid.Visibility = Visibility.Visible;
                      MainPage._this.ConnectServer.IsEnabled = false;
                      MainPage._this.IsSubscribed.IsEnabled = false;
                      MainPage._this.Subscribe.IsEnabled = false;
                      MainPage._this.Unsubscribe.IsEnabled = false;


                  });
            }

            public void sendResponse(String username, String password)
            {
                Dictionary<String, String> parms = new Dictionary<String, String>();
                parms.Add("j_username", username);
                parms.Add("j_password", password);

                submitLoginForm("j_security_check", parms, null, 10000, "post");
            }

            public override bool isCustomResponse(WLResponse response)
            {
                if (response == null || response.getResponseText() == null ||
                    !response.getResponseText().Contains("j_security_check"))
                {
                    return false;
                }
                else
                {
                    return true;
                }

            }

            public override void onSuccess(WLResponse resp)
            {
                Debug.WriteLine("Challenge handler success");

                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    MainPage._this.LoginGrid.Visibility = Visibility.Collapsed;
                    MainPage._this.ConnectServer.IsEnabled = true;
                    MainPage._this.IsSubscribed.IsEnabled = true;
                    MainPage._this.Subscribe.IsEnabled = true;
                    MainPage._this.Unsubscribe.IsEnabled = true;

                });

                submitSuccess(resp);
            }

            public override void onFailure(WLFailResponse failResp)
            {
                Debug.WriteLine("Challenge handler failure ");
            }
        }

        class OnReadyToSubscribeListener : WLOnReadyToSubscribeListener, WLEventSourceListener
        {
            public void onReadyToSubscribe()
            {
                Debug.WriteLine("On ready to subscribe");
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    MainPage._this.Console.Text = "\nOn ready to subscribe";
                    MainPage._this.Subscribe.IsEnabled = true;
                    MainPage._this.IsSubscribed.IsEnabled = true;
                    MainPage._this.Unsubscribe.IsEnabled = true;

                });
                try
                {
                    WLClient.getInstance().getPush().registerEventSourceCallback("myPush", "PushAdapter", "PushEventSource", this);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            public void onReceive(String props, String payload)
            {
                Debug.WriteLine("Props: " + props);
                Debug.WriteLine("Payload: " + payload);

                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    MainPage._this.Console.Text = "Received Message\nprops:" + props + "\npayload:" + payload;
                });

            }

        }

        class MySubscribeListener : WLResponseListener
        {
            public void onSuccess(WLResponse resp)
            {
                Debug.WriteLine("Subscribe success " + resp.getResponseText());
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    MainPage._this.Console.Text = "\n\nSubscribed successfully";
                });
            }

            public void onFailure(WLFailResponse resp)
            {
                Debug.WriteLine("Subscribe failure" + resp.getResponseText());
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    MainPage._this.Console.Text = "\n\nFailed to subscribe." + "\n" + resp.getErrorMsg();
                });
            }
        }
        class MyUnsubscribeListener : WLResponseListener
        {
            public void onSuccess(WLResponse resp)
            {
                Debug.WriteLine("Unsubscribe success " + resp.getResponseText());
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    MainPage._this.Console.Text = "\n\nUnsubscribed Successfully";
                });
            }

            public void onFailure(WLFailResponse resp)
            {
                Debug.WriteLine("Unsubscribe failure" + resp.getResponseText());
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    MainPage._this.Console.Text = "\n\nFailed to unsubscribe" + "\n" + resp.getErrorMsg();
                });
            }
        }

        private void ClearConsole(object sender, DoubleTappedRoutedEventArgs e)
        {
            Console.Text = "";
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainPage.CustomChallengeHandler)MainPage._this.getChallengeHandler()).sendResponse(username.Text, password.Text);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            async () =>
            {
                MainPage._this.LoginGrid.Visibility = Visibility.Collapsed;
            });
            ((MainPage.CustomChallengeHandler)MainPage._this.getChallengeHandler()).sendResponse("", "");
        }

        private void ShowConsole(object sender, TappedRoutedEventArgs e)
        {
            MainPage._this.ConsolePanel.Visibility = Visibility.Visible;
            MainPage._this.InfoPanel.Visibility = Visibility.Collapsed;
            MainPage._this.ConsoleTab.Foreground = new SolidColorBrush(Colors.DodgerBlue);
            MainPage._this.InfoTab.Foreground = new SolidColorBrush(Colors.Gray);
        }

        private void ShowInfo(object sender, TappedRoutedEventArgs e)
        {
            MainPage._this.ConsolePanel.Visibility = Visibility.Collapsed;
            MainPage._this.InfoPanel.Visibility = Visibility.Visible;
            MainPage._this.InfoTab.Foreground = new SolidColorBrush(Colors.DodgerBlue);
            MainPage._this.ConsoleTab.Foreground = new SolidColorBrush(Colors.Gray);
        }
    }
}
