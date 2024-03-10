using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace GameVaultLibrary
{
    public partial class GameVaultLibrarySettingsView : UserControl
    {
        private readonly GameVaultLibrarySettingsViewModel ViewModel;

        public GameVaultLibrarySettingsView(GameVaultLibrarySettingsViewModel viewModel)
        {
            InitializeComponent();

            this.ViewModel = viewModel;

            pwd.Password = viewModel.Settings.Password;

            pwd.PasswordChanged += PWD_PasswordChanged;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameVaultLibrarySettingsViewModel.Settings))
                pwd.Password = ViewModel.Settings.Password;
        }

        private void PWD_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Settings.Password = pwd.Password;
        }
    }
}