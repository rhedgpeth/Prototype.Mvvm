using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Prototype.Mvvm.ViewModels;
using Xamarin.Forms;

namespace Prototype.Mvvm.Services
{
    public interface IViewFor
    {
        object ViewModel { get; set; }
    }

    public interface IViewFor<T> : IViewFor  where T : BaseViewModel
    {
        new T ViewModel { get; set; }
    }

    public class NavigationService : INavigationService
    {
        INavigation FormsNavigation => Application.Current.MainPage.Navigation;

        // View model to view lookup - making the assumption that view model to view will always be 1:1
        readonly Dictionary<Type, Type> _viewModelViewDictionary = new Dictionary<Type, Type>();

        #region Registration

        public void AutoRegister(Assembly asm)
        {
            // Loop through everything in the assembly that implements IViewFor<T>
            foreach (var type in asm.DefinedTypes.Where(dt => !dt.IsAbstract &&
                        dt.ImplementedInterfaces.Any(ii => ii == typeof(IViewFor))))
            {
                // Get the IViewFor<T> portion of the type that implements it
                var viewForType = type.ImplementedInterfaces.FirstOrDefault(
                    ii => ii.IsConstructedGenericType &&
                    ii.GetGenericTypeDefinition() == typeof(IViewFor<>));
                    
                // Register it, using the T as the key and the view as the value
                Register(viewForType.GenericTypeArguments[0], type.AsType());

                ServiceContainer.Register(viewForType.GenericTypeArguments[0], viewForType.GenericTypeArguments[0], true);
            }
        }

        public void Register(Type viewModelType, Type viewType) 
        {
            if (!_viewModelViewDictionary.ContainsKey(viewModelType))
            {
                _viewModelViewDictionary.Add(viewModelType, viewType);
            }
        }

        #endregion

        #region Replace

        // Because we're going to do a hard switch of the page, either return
        // the detail page, or if that's null, then the current main page       
        Page DetailPage
        {
            get
            {
                var masterController = Application.Current.MainPage as MasterDetailPage;
                return masterController?.Detail ?? Application.Current.MainPage;
            }
            set
            {
                if (Application.Current.MainPage is MasterDetailPage masterController)
                {
                    masterController.Detail = value;
                    masterController.IsPresented = false;
                }
                else
                {
                    Application.Current.MainPage = value;
                }
            }
        }

        public async Task ReplaceAsync(BaseViewModel viewModel, bool allowSamePageSet = false)
        {
            // Ensure that we're not pushing a new page if the DetailPage is already set to this type
            if (!allowSamePageSet)
            {
                IViewFor page;

                if (DetailPage is NavigationPage)
                {
                    page = ((NavigationPage)DetailPage).RootPage as IViewFor;
                }
                else
                {
                    page = DetailPage as IViewFor;
                }

                if (page?.ViewModel.GetType() == viewModel.GetType())
                {
                    var masterController = Application.Current.MainPage as MasterDetailPage;

                    masterController.IsPresented = false;

                    return;
                }
            }

            Page newDetailPage = await Task.Run(() =>
            {
                var view = InstantiateView(viewModel);

                // Tab pages shouldn't go into navigation pages
                if (view is TabbedPage)
                {
                    newDetailPage = (Page)view;
                }
                else
                {
                    newDetailPage = new NavigationPage((Page)view);
                }

                return newDetailPage;
            });

            DetailPage = newDetailPage;
        }

        public async Task ReplaceAsync<T>(Action<T> initialize = null) where T : BaseViewModel
        {
            T viewModel = await Task.Run(() =>
            {
                // First instantiate the view model
                viewModel = Activator.CreateInstance<T>();

                initialize?.Invoke(viewModel);

                return viewModel;
            }).ConfigureAwait(false);

            // Actually switch the page
            await ReplaceAsync(viewModel);
        }


        public void ReplaceRoot(BaseViewModel viewModel, bool withNavigationEnabled = true)
        {
            if (InstantiateView(viewModel) is Page view)
            {
                if (withNavigationEnabled)
                {
                    Application.Current.MainPage = new NavigationPage(view);
                }
                else
                {
                    Application.Current.MainPage = view;
                }
            }
        }

        #endregion

        #region Pop

        public Task PopAsync(bool animated = true) => FormsNavigation.PopAsync(animated);

        public Task PopModalAsync(bool animated = true) => FormsNavigation.PopModalAsync(animated);

        public Task PopToRootAsync(bool animated = true) => FormsNavigation.PopToRootAsync(animated);

        #endregion

        #region Push

        public Task PushAsync(BaseViewModel viewModel) => FormsNavigation.PushAsync((Page)InstantiateView(viewModel));

        public Task PushAsync<T>(Action<T> initialize = null) where T : BaseViewModel
        {
            T viewModel;

            // Instantiate the view model & invoke the initialize method, if any
            viewModel = Activator.CreateInstance<T>();

            initialize?.Invoke(viewModel);

            return PushAsync(viewModel);
        }

        public Task PushModalAsync(BaseViewModel viewModel, bool nestedNavigation = false)
        {
            var view = InstantiateView(viewModel);

            Page page;

            if (nestedNavigation)
            {
                page = new NavigationPage((Page)view);
            }
            else
            {
                page = (Page)view;
            }

            return FormsNavigation.PushModalAsync(page);
        }

        public Task PushModalAsync<T>(Action<T> initialize = null) where T : BaseViewModel
        {
            T viewModel;

            viewModel = Activator.CreateInstance<T>();

            initialize?.Invoke(viewModel);

            return PushModalAsync(viewModel);
        }

        #endregion

        IViewFor InstantiateView(BaseViewModel viewModel)
        {
            // Figure out what type the view model is
            var viewModelType = viewModel.GetType();

            // Look up what type of view it corresponds to
            var viewType = _viewModelViewDictionary[viewModelType];

            // Instantiate it
            var view = (IViewFor)Activator.CreateInstance(viewType);

            view.ViewModel = viewModel;

            return view;
        }
    }
}