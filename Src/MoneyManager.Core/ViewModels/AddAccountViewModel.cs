﻿using Cirrious.MvvmCross.ViewModels;
using MoneyManager.Foundation.Model;
using MoneyManager.Foundation.OperationContracts;
using PropertyChanged;

namespace MoneyManager.Core.ViewModels
{
    [ImplementPropertyChanged]
    public class AddAccountViewModel : BaseViewModel
    {
        private readonly IRepository<Account> accountRepository;

        public AddAccountViewModel(IRepository<Account> accountRepository)
        {
            this.accountRepository = accountRepository;

            SaveCommand = new MvxCommand(SaveAccount);
            DeleteCommand = new MvxCommand(DeleteAccount);
            CancelCommand = new MvxCommand(Cancel);
        }

        /// <summary>
        ///     Saves all changes to the database
        ///     or creates a new account depending on
        ///     the <see cref="IsEdit" /> property
        /// </summary>
        public MvxCommand SaveCommand { get; set; }

        /// <summary>
        ///     Deletes the selected account from the database
        /// </summary>
        public MvxCommand DeleteCommand { get; set; }

        /// <summary>
        ///     Cancels the operation and will revert the changes
        /// </summary>
        public MvxCommand CancelCommand { get; set; }

        /// <summary>
        ///     indicates if the account already exists and shall
        ///     be updated or new created
        /// </summary>
        public bool IsEdit { get; set; }

        /// <summary>
        ///     The currently selected account
        /// </summary>
        public Account SelectedAccount
        {
            get { return accountRepository.Selected; }
            set { accountRepository.Selected = value; }
        }

        private void SaveAccount()
        {
            accountRepository.Save(accountRepository.Selected);
            Close(this);
        }

        private void DeleteAccount()
        {
            accountRepository.Delete(accountRepository.Selected);
            Close(this);
        }

        private void Cancel()
        {
            //TODO: revert changes
            Close(this);
        }
    }
}