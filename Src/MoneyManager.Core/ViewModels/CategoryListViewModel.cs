﻿using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;
using MoneyManager.Foundation;
using MoneyManager.Foundation.Model;
using MoneyManager.Foundation.OperationContracts;
using PropertyChanged;
using IDialogService = MoneyManager.Foundation.OperationContracts.IDialogService;

namespace MoneyManager.Core.ViewModels
{
    [ImplementPropertyChanged]
    public class CategoryListViewModel : ViewModelBase
    {
        private readonly IRepository<Category> categoryRepository;
        private readonly IDialogService dialogService;
        private readonly INavigationService navigationService;
        private readonly ITransactionRepository transactionRepository;

        private string searchText;

        /// <summary>
        ///     Creates an CategoryListViewModel
        /// </summary>
        /// <param name="categoryRepository">An instance of <see cref="IRepository{T}" /> of type category.</param>
        /// <param name="transactionRepository">An instance of <see cref="ITransactionRepository" />.</param>
        /// <param name="navigationService">An instance of <see cref="INavigationService" /></param>
        /// <param name="dialogService">An instance of <see cref="IDialogService" /></param>
        public CategoryListViewModel(IRepository<Category> categoryRepository,
            ITransactionRepository transactionRepository,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            this.categoryRepository = categoryRepository;
            this.transactionRepository = transactionRepository;
            this.navigationService = navigationService;
            this.dialogService = dialogService;

            DeleteCategoryCommand = new RelayCommand<Category>(DeleteCategory);

            Categories = AllCategories;
        }

        /// <summary>
        ///     Deletes the passed Category after show a confirmation dialog.
        /// </summary>
        public RelayCommand<Category> DeleteCategoryCommand { get; set; }

        /// <summary>
        ///     Indicates wether the view is shown from the settings to adjust something
        ///     or from the transaction modificatio nto select a category.
        /// </summary>
        public bool IsSettingCall { get; set; }
        public ObservableCollection<Category> Categories { get; set; }

        private ObservableCollection<Category> AllCategories => categoryRepository.Data;
        
        /// <summary>
        ///     The currently selected category. If IsSettingsCall is set ît will 
        ///     return and set the selected item in the CategoryRepository, otherwise
        ///     the category of the selected transaction.
        /// </summary>
        public Category SelectedCategory
        {
            get
            {
                var selected = IsSettingCall
                    ? categoryRepository.Selected
                    : transactionRepository.Selected.Category;

                return selected == null
                    ? new Category()
                    : transactionRepository.Selected.Category;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                if (IsSettingCall)
                {
                    categoryRepository.Selected = value;
                }
                else
                {
                    transactionRepository.Selected.Category = value;
                    navigationService.GoBack();
                }
            }
        }

        /// <summary>
        ///     Text to search for. Will perform the search when the text changes.
        /// </summary>
        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                Search();
            }
        }

        /// <summary>
        ///     Performs a search with the text in the searchtext property
        /// </summary>
        public void Search()
        {
            if (SearchText != string.Empty)
            {
                Categories = new ObservableCollection<Category>
                    (AllCategories.Where(x => x.Name != null && x.Name.ToLower().Contains(searchText.ToLower()))
                        .ToList());
            }
            else
            {
                Categories = AllCategories;
            }
        }

        private async void DeleteCategory(Category categoryToDelete)
        {
            if (await dialogService.ShowConfirmMessage(Strings.DeleteTitle, Strings.DeleteConfirmationMessage))
            {
                categoryRepository.Delete(categoryToDelete);
            }
        }
    }
}