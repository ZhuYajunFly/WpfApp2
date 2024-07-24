using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2
{
    public partial class MainWindowVM:ObservableObject
    {
        [ObservableProperty]
        private string id = "666";

        [RelayCommand]
        private void ChangeId()
        {
            Id = DateTime.Now.ToString("ss.fff");
        }
    }
}
