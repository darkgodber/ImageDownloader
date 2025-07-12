using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImageDownloader.ViewModels
{
    /// <summary>
    /// Базовый класс для всех ViewModel’ей:
    /// - Реализует INotifyPropertyChanged
    /// - Хранит SynchronizationContext UI-потока, чтобы PropertyChanged всегда шёл в UI
    /// - Предоставляет SetProperty для безопасного обновления полей
    /// - Добавляет методы для массового «всплеска» уведомлений
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        // Куда мы будем маршалить вызовы OnPropertyChanged, чтобы они были в UI-потоке
        private readonly SynchronizationContext _syncContext;

        protected BaseViewModel()
        {
            // При создании запомним текущий контекст (в WPF это UI context)
            _syncContext = SynchronizationContext.Current;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Вызывает событие PropertyChanged. Если мы в фоновом потоке —
        /// постим вызов обратно в UI-поток.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            // Если у нас есть сохранённый UI-контекст и мы не в нём — маршалим
            if (_syncContext != null && SynchronizationContext.Current != _syncContext)
            {
                _syncContext.Post(_ => handler(this, new PropertyChangedEventArgs(propertyName)), null);
            }
            else
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Устанавливает поле и, если оно изменилось, кидает PropertyChanged для данного свойства.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Генерирует PropertyChanged для нескольких свойств сразу.
        /// Полезно, если после одной операции поменялись несколько зависимых свойств.
        /// </summary>
        protected void RaisePropertiesChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
                OnPropertyChanged(name);
        }

        /// <summary>
        /// Опасная штука: вызывает PropertyChanged для **всех** публичных свойств.
        /// Можно юзать для «глобального обновления» View, но нечасто.
        /// </summary>
        protected void RaiseAllPropertiesChanged()
        {
            var props = GetType().GetProperties();
            foreach (var p in props)
            {
                if (p.CanRead)
                    OnPropertyChanged(p.Name);
            }
        }

        #endregion
    }
}
