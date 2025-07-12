using System;
using System.Windows.Input;

namespace ImageDownloader.ViewModels
{
    /// <summary>
    /// Универсальная реализация ICommand, которая делегирует выполнение
    /// и проверку возможности выполнения вызванным методам.
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Fields

        // Делегат, выполняющийся при вызове Execute
        private readonly Action<object?> _execute;

        // Делегат, определяющий, можно ли выполнить команду сейчас
        private readonly Predicate<object?>? _canExecute;

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт команду с указанными методами выполнения и проверки.
        /// </summary>
        /// <param name="execute">
        /// Обязательный делегат, который будет вызываться в Execute().
        /// </param>
        /// <param name="canExecute">
        /// Необязательный делегат, который возвращает true, если команду
        /// можно выполнить; по умолчанию всегда возвращает true.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Выбрасывается, если передан null в execute.
        /// </exception>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        #endregion

        #region ICommand Members

        /// <summary>
        /// Событие, которое WPF подписывает под RequerySuggested,
        /// чтобы автоматически обновлять состояние IsEnabled у кнопок.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Возвращает true, если команда может быть выполнена.
        /// Вызывает _canExecute, если он задан, иначе — true.
        /// </summary>
        public bool CanExecute(object? parameter)
            => _canExecute?.Invoke(parameter) ?? true;

        /// <summary>
        /// Вызывает делегат _execute, выполняющий логику команды.
        /// </summary>
        public void Execute(object? parameter)
            => _execute(parameter);

        #endregion

        #region Public Methods

        /// <summary>
        /// Принудительно запускает обновление состояния CanExecute
        /// (например, когда внешнее условие изменилось).
        /// </summary>
        public void RaiseCanExecuteChanged()
            => CommandManager.InvalidateRequerySuggested();

        #endregion
    }
}
