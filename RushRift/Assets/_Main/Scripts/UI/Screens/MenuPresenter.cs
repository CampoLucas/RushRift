using Game.DesignPatterns.Observers;

namespace Game.UI.Screens
{
    public abstract class MenuPresenter<TModel, TView> : UIPresenter<TModel, TView>, ISubject<MenuState>
        where TModel : UIModel 
        where TView : UIView
    {
        private ISubject<MenuState> _subject = new Subject<MenuState>();
        
        public bool Attach(IObserver<MenuState> observer)
        {
            return _subject.Attach(observer);
        }

        public bool Detach(IObserver<MenuState> observer)
        {
            return _subject.Detach(observer);
        }

        public void DetachAll()
        {
            _subject.DetachAll();
        }

        public void NotifyAll(MenuState arg)
        {
            _subject.NotifyAll(arg);
        }

        public override void Dispose()
        {
            _subject.DetachAll();
            _subject = null;
            
            base.Dispose();
        }
    }
}