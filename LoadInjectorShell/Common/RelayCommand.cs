using System;
using System.Diagnostics;
using System.Windows.Input;

namespace LoadInjector.Common {

    public class RelayCommand<T> : ICommand {

        #region Fields

        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        #endregion Fields

        #region Constructors

        public RelayCommand(Action<T> action)
            : this(action, null) {
        }

        public RelayCommand(Action<T> execute, Predicate<T> canExecute) {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }

        #endregion Constructors

        #region ICommand Members

        [DebuggerStepThrough]
        public bool CanExecute(object parameter) {
            //return _canExecute == null ? true : _canExecute((T)parameter);

            if (_canExecute == null) {
                return true;
            }
            if (_canExecute != null) {
                if (parameter is T) {
                    return _canExecute((T)parameter);
                }
                //For triggerng even user passes null parameter.
                else if (parameter == null) {
                    return _canExecute(default(T));
                }
            }

            return true;
        }

        public event EventHandler CanExecuteChanged {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object parameter) {
            _execute((T)parameter);
        }

        #endregion ICommand Members
    }

    public class RelayCommand : RelayCommand<object> {

        public RelayCommand(Action<object> action)
            : base(action) {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
            : base(execute, canExecute) { }
    }
}