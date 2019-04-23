using System.Threading.Tasks;

namespace Prototype.Mvvm.ViewModels
{
    public abstract class BaseViewModel : BaseNotify
    {
        bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetPropertyChanged(ref _isBusy, value);
        }

        public virtual Task InitAsync() => Task.FromResult(true);

        public virtual Task LoadAsync(bool refresh) => Task.FromResult(true);
    }
}
