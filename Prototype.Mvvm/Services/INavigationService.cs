using System;
using System.Reflection;
using System.Threading.Tasks;
using Prototype.Mvvm.ViewModels;

namespace Prototype.Mvvm.Services
{
    public interface INavigationService
    {
        void AutoRegister(Assembly asm);

        void Register(Type viewModelType, Type viewType);

        Task PopAsync(bool animated = true);
        Task PopModalAsync(bool animated = true);
        Task PushAsync(BaseViewModel viewModel);
        Task PushAsync<T>(Action<T> initialize = null) where T : BaseViewModel;
        Task PushModalAsync<T>(Action<T> initialize = null) where T : BaseViewModel;
        Task PushModalAsync(BaseViewModel viewModel, bool nestedNavigation = false);
        Task PopToRootAsync(bool animated = true);

        Task ReplaceAsync<T>(Action<T> initialize = null) where T : BaseViewModel;
        Task ReplaceAsync(BaseViewModel viewModel, bool allowSamePageSet = false);

        void ReplaceRoot(BaseViewModel viewModel, bool withNavigationEnabled = true);
    }
}
