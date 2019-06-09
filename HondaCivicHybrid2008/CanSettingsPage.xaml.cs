using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace HondaCivicHybrid2008
{
    public partial class CanSettingsPage : PhoneApplicationPage
    {
        public CanSettingsPage()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            txtCanSettings.Text = App.canCommands;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.canCommands = txtCanSettings.Text;
        }
    }
}